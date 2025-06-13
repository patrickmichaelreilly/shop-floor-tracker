# System Architecture Document
## Shop Floor Part Tracking System

**Document Version:** 1.0  
**Date:** 2025-01-13  
**Author:** Claude Code  
**Status:** Draft

---

## Executive Summary

This document defines the technical architecture for the Shop Floor Part Tracking System, an ASP.NET Core 8.0 web application designed to replace Production Coach software. The system implements Clean Architecture principles with clear separation of concerns, supporting multi-station workflows and real-time part tracking.

---

## High-Level Architecture Overview

### System Context Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Shop Floor Environment                            │
├─────────────────┬───────────────────┬───────────────────┬──────────────────┤
│  Sorting Station│  Assembly Station │  Shipping Station │  Admin Terminal  │
│                 │                   │                   │                  │
│ ┌─────────────┐ │ ┌─────────────┐   │ ┌─────────────┐   │ ┌──────────────┐ │
│ │   Browser   │ │ │   Browser   │   │ │   Browser   │   │ │   Browser    │ │
│ │   Terminal  │ │ │   Terminal  │   │ │   Terminal  │   │ │   Terminal   │ │
│ └─────────────┘ │ └─────────────┘   │ └─────────────┘   │ └──────────────┘ │
└─────────────────┴───────────────────┴───────────────────┴──────────────────┘
          │                    │                    │                 │
          └────────────────────┼────────────────────┼─────────────────┘
                               │                    │
                    ┌──────────┴────────────────────┴──────────┐
                    │         ASP.NET Core 8.0 Web App        │
                    │          (Windows Service)               │
                    │                                          │
                    │  ┌─────────────┐    ┌─────────────┐     │
                    │  │   SignalR   │    │ Background  │     │
                    │  │   Hub       │    │ Services    │     │
                    │  └─────────────┘    └─────────────┘     │
                    └──────────┬───────────────────┬──────────┘
                               │                   │
              ┌────────────────┴──────────────┐    │
              │       Database Layer          │    │
              │                               │    │
              │ ┌─────────────┐ ┌───────────┐ │    │
              │ │   SQLite    │ │SQL Server │ │    │
              │ │    (Dev)    │ │Expr (Prod)│ │    │
              │ └─────────────┘ └───────────┘ │    │
              └───────────────────────────────┘    │
                                                   │
              ┌────────────────────────────────────┴─────┐
              │           File System                    │
              │                                          │
              │ ┌─────────────┐    ┌─────────────┐       │
              │ │SQL CE Import│    │   Archive   │       │
              │ │   Folder    │    │   Folder    │       │
              │ └─────────────┘    └─────────────┘       │
              └──────────────────────────────────────────┘
```

### Technology Stack Overview

| Layer | Technology | Justification |
|-------|------------|---------------|
| **Frontend** | Razor Pages + Bootstrap 5 | Server-side rendering for shop floor terminals; minimal JavaScript complexity |
| **Backend** | ASP.NET Core 8.0 | Windows server requirement; mature ecosystem; excellent performance |
| **Database** | SQLite (Dev) / SQL Server Express (Prod) | File-based development; Windows-integrated production |
| **ORM** | Entity Framework Core 8.0 | Strong .NET integration; migration support; LINQ capabilities |
| **Real-time** | SignalR | Native ASP.NET Core integration; WebSocket fallback |
| **Hosting** | Windows Service | Production requirement; automatic startup; Windows integration |

---

## Clean Architecture Implementation

### Architecture Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                    Presentation Layer                           │
│              ShopFloorTracker.Web                              │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │Razor Pages  │ │Controllers  │ │SignalR Hubs │ │ViewModels │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                   Application Layer                             │
│             ShopFloorTracker.Application                       │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │  Commands   │ │   Queries   │ │  Services   │ │   DTOs    │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                     Domain Layer                                │
│               ShopFloorTracker.Core                            │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │  Entities   │ │Domain Logic │ │Repositories │ │   Enums   │ │
│  │             │ │  Services   │ │(Interfaces) │ │           │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                 Infrastructure Layer                            │
│            ShopFloorTracker.Infrastructure                     │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌───────────┐ │
│  │EF Core Data │ │File Import  │ │Repositories │ │Background │ │
│  │   Context   │ │  Services   │ │(Concrete)   │ │ Services  │ │
│  └─────────────┘ └─────────────┘ └─────────────┘ └───────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Layer Responsibilities

#### 1. Presentation Layer (ShopFloorTracker.Web)
**Purpose:** User interface and HTTP request handling

**Components:**
- **Razor Pages:** Station-specific interfaces with server-side rendering
- **Controllers:** API endpoints for AJAX calls and real-time updates
- **SignalR Hubs:** Real-time communication for status updates
- **ViewModels:** Data transfer objects for UI binding
- **wwwroot:** Static assets (CSS, JavaScript, images)

**Key Patterns:**
- Page-based routing for station workflows
- Model binding for form submissions
- Real-time event broadcasting
- Responsive design for various screen sizes

#### 2. Application Layer (ShopFloorTracker.Application)
**Purpose:** Business workflows and use case orchestration

**Components:**
- **Commands:** Write operations (scan part, import work order)
- **Queries:** Read operations (get part status, list products)
- **Services:** Business workflow coordination
- **DTOs:** Data transfer objects between layers
- **Validators:** Input validation and business rule enforcement

**Key Patterns:**
- Command Query Responsibility Segregation (CQRS)
- Mediator pattern for request handling
- Service layer for complex business operations
- Dependency injection for service resolution

#### 3. Domain Layer (ShopFloorTracker.Core)
**Purpose:** Business entities and core business logic

**Components:**
- **Entities:** Core business objects (WorkOrder, Product, Part)
- **Domain Services:** Business logic that doesn't fit in entities
- **Repository Interfaces:** Data access contracts
- **Enums:** Business constants and status values
- **Domain Events:** Business event definitions

**Key Patterns:**
- Domain-driven design (DDD) entities
- Rich domain models with behavior
- Repository pattern interfaces
- Domain events for loose coupling

#### 4. Infrastructure Layer (ShopFloorTracker.Infrastructure)
**Purpose:** External service integration and data persistence

**Components:**
- **Data Context:** Entity Framework Core database context
- **Repository Implementations:** Concrete data access classes
- **File Import Services:** SQL CE file processing
- **Background Services:** Long-running tasks
- **External Service Integrations:** Future extensibility

**Key Patterns:**
- Repository pattern implementation
- Unit of Work for transaction management
- Background service pattern
- Configuration-based service registration

---

## Database Architecture

### Database Strategy

#### Development Environment
- **Database:** SQLite
- **Location:** Local file in project directory
- **Benefits:** Zero configuration, version control friendly, fast setup
- **Connection:** Single file database for each developer

#### Production Environment
- **Database:** SQL Server Express
- **Location:** Windows server installation
- **Benefits:** Better performance, Windows integration, backup capabilities
- **Connection:** Network-based with connection pooling

### Entity Framework Core Implementation

#### DbContext Configuration
```csharp
public class ShopFloorDbContext : DbContext
{
    // DbSet properties for all entities
    // OnModelCreating for configuration
    // Database initialization and seeding
}
```

#### Migration Strategy
- **Initial Migration:** Complete database schema creation
- **Seed Data Migration:** Default process stations and settings
- **Version Migrations:** Schema changes and data updates
- **Environment-Specific:** Different connection strings per environment

#### Performance Optimizations
- Indexed foreign keys for common queries
- Materialized views for complex reporting
- Connection pooling for concurrent access
- Query optimization and profiling

---

## Station Interface Design

### Common Design Principles

#### Responsive Layout Framework
```css
/* Mobile-first responsive design */
@media (min-width: 768px) { /* Tablet */ }
@media (min-width: 992px) { /* Desktop */ }
@media (min-width: 1200px) { /* Large screens */ }
```

#### Touch-Friendly Interface Standards
- **Minimum Button Size:** 44px × 44px
- **Touch Targets:** 10px spacing between interactive elements
- **High Contrast:** Dark text on light backgrounds
- **Large Fonts:** Minimum 16px for body text, 24px for headings

#### Barcode Scanning Integration
- **Input Method:** Hardware barcode scanners acting as keyboards
- **Focus Management:** Auto-focus on scan input fields
- **Validation:** Real-time barcode format validation
- **Feedback:** Immediate visual and audio feedback

### Station-Specific Interfaces

#### 1. Admin Interface (`/Admin/`)
**Purpose:** System administration and work order management

**Key Pages:**
- `/Admin/Dashboard` - System overview and statistics
- `/Admin/Import` - Work order import from SQL CE files
- `/Admin/Racks` - Storage rack configuration
- `/Admin/Users` - User management and permissions
- `/Admin/Settings` - System configuration

**Features:**
- File upload and import status tracking
- Rack configuration with visual layout editor
- Real-time system monitoring dashboard
- User role and station assignment management

#### 2. Sorting Station Interface (`/Sorting/`)
**Purpose:** Part scanning and storage assignment

**Key Pages:**
- `/Sorting/Scan` - Primary barcode scanning interface
- `/Sorting/Assignments` - Current storage assignments
- `/Sorting/Rack/{rackId}` - Visual rack layout and status

**Features:**
- Large barcode input field with auto-focus
- Real-time storage slot assignment display
- Visual rack utilization indicators
- Product grouping logic visualization
- Error handling for invalid scans

**Interface Layout:**
```
┌─────────────────────────────────────────────────┐
│  [SCAN PART BARCODE]                            │
│  ┌─────────────────────────────────────────────┐ │
│  │         Large Input Field               [🔍] │ │
│  └─────────────────────────────────────────────┘ │
│                                                 │
│  Last Scanned: Part #12345                     │
│  Assigned to: Rack A-12-05                     │
│                                                 │
│  ┌─────────────────────────────────────────────┐ │
│  │           Storage Assignment                │ │
│  │     🏢 Rack A, Row 12, Column 5           │ │
│  │         Product: Kitchen Cabinet            │ │
│  └─────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────┘
```

#### 3. Assembly Station Interface (`/Assembly/`)
**Purpose:** Product assembly scanning and subassembly coordination

**Key Pages:**
- `/Assembly/Scan` - Product assembly scanning
- `/Assembly/Locations` - Subassembly location display
- `/Assembly/Ready` - Products ready for assembly

**Features:**
- Product barcode scanning with validation
- Subassembly location mapping and directions
- Assembly readiness status indicators
- Pick list generation with optimal routing

#### 4. Shipping Station Interface (`/Shipping/`)
**Purpose:** Final product verification and work order completion

**Key Pages:**
- `/Shipping/Scan` - Product shipping verification
- `/Shipping/WorkOrder/{id}` - Complete work order checklist
- `/Shipping/Documentation` - Shipping document generation

**Features:**
- Product verification scanning
- Work order completion checklist
- Hardware and detached product tracking
- Shipping documentation generation

---

## Real-Time Communication Architecture

### SignalR Hub Implementation

#### Hub Structure
```csharp
public class ShopFloorHub : Hub
{
    // Connection management by station
    // Group-based message broadcasting
    // Authentication and authorization
}
```

#### Real-Time Events
- **PartScanned:** Broadcast part status changes
- **StorageAssigned:** Notify of new storage assignments
- **ProductReady:** Alert when products ready for next station
- **SystemAlert:** Broadcast system-wide notifications

#### Client Groups
- **Admin:** System administrators receiving all events
- **SortingStation:** Part scanning and assignment events
- **AssemblyStation:** Assembly readiness and location events
- **ShippingStation:** Shipping verification and completion events

---

## Background Services Architecture

### File Import Service

#### Monitoring Strategy
```csharp
public class FileImportService : BackgroundService
{
    // FileSystemWatcher for folder monitoring
    // Queue-based processing for reliability
    // Error handling and retry logic
    // Progress tracking and notifications
}
```

#### Import Process Flow
1. **File Detection:** Monitor designated import folder
2. **File Validation:** Verify SQL CE format and structure
3. **Data Parsing:** Extract hierarchical work order data
4. **Database Import:** Transactional import with rollback capability
5. **File Archival:** Move processed files to archive folder
6. **Notification:** Broadcast import completion status

### Database Maintenance Service

#### Maintenance Tasks
- **Audit Log Cleanup:** Remove old process history records
- **Performance Monitoring:** Track query performance and optimization
- **Index Maintenance:** Rebuild fragmented indexes
- **Statistics Updates:** Refresh database statistics for query optimization

---

## Security Architecture

### Authentication Strategy

#### Development Phase
- **Simple Authentication:** Username/password with session management
- **Role-Based Access:** Admin, Operator, Manager roles
- **Station Assignment:** Users assigned to specific stations

#### Production Enhancement (Future)
- **Windows Authentication:** Integration with domain accounts
- **Active Directory:** Centralized user management
- **Multi-Factor Authentication:** Enhanced security for admin functions

### Authorization Model

#### Role Definitions
- **Administrator:** Full system access, configuration management
- **Station Operator:** Station-specific scanning and operations
- **Production Manager:** Read-only access to all stations, reporting
- **Quality Assurance:** Audit trail access, exception reporting

### Input Security
- **SQL Injection Prevention:** Parameterized queries and Entity Framework
- **Barcode Validation:** Format validation and sanitization
- **File Upload Security:** Type validation and virus scanning
- **Cross-Site Scripting (XSS):** Input encoding and Content Security Policy

---

## Performance Architecture

### Database Performance

#### Query Optimization
- **Indexed Queries:** All foreign keys and frequent query fields indexed
- **Query Profiling:** Entity Framework query logging and analysis
- **Batch Operations:** Bulk insert/update for large data sets
- **Connection Pooling:** Efficient database connection management

#### Caching Strategy
- **Memory Caching:** Frequently accessed configuration data
- **Response Caching:** Static content and infrequent data
- **Session State:** User-specific temporary data
- **SignalR Backplane:** Scale-out capability for multiple servers

### Application Performance

#### Response Time Targets
- **Barcode Scanning:** < 500ms response time
- **Page Load:** < 2 seconds for initial load
- **Real-time Updates:** < 100ms for SignalR notifications
- **File Import:** Progress feedback every 5 seconds

#### Scalability Considerations
- **Concurrent Users:** Support 20+ simultaneous station operations
- **Database Load:** Handle 1000+ part scans per hour
- **File Processing:** Process work orders with 5000+ parts
- **Memory Usage:** Efficient memory management for long-running service

---

## Deployment Architecture

### Development Environment

#### Local Development Setup
```bash
# Development database (SQLite)
# File system monitoring (local folders)
# Hot reload and debugging
# Test data seeding
```

#### Development Tools
- **Visual Studio 2022:** Primary IDE with debugging
- **SQL Server Management Studio:** Database administration
- **Postman:** API testing and validation
- **Browser DevTools:** Frontend debugging and performance

### Production Environment

#### Windows Service Deployment
```xml
<!-- Service configuration -->
<ServiceHost>
    <DatabaseConnection>SQL Server Express</DatabaseConnection>
    <FileImportPath>\\server\import</FileImportPath>
    <LoggingLevel>Information</LoggingLevel>
</ServiceHost>
```

#### Production Configuration
- **IIS Integration:** Reverse proxy for web requests
- **Windows Service:** Background processing and monitoring
- **File Share Access:** Network folder monitoring capability
- **Logging and Monitoring:** Application insights and error tracking

---

## Integration Architecture

### External System Integration

#### Microvellum Integration
- **File Format:** SQL CE database files
- **Import Schedule:** On-demand and scheduled imports
- **Data Mapping:** Preserve hierarchical structure and identifiers
- **Error Handling:** Graceful failure with notification

#### Future Integration Points
- **ERP Systems:** Work order synchronization
- **CNC Machine Integration:** Automated cut status updates
- **Label Printing:** Custom label generation
- **Reporting Systems:** Data export and analytics

### API Design (Internal)

#### RESTful Endpoints
```
GET    /api/parts/{id}/status
POST   /api/parts/{id}/scan
GET    /api/storage/racks/{id}/availability
POST   /api/workorders/import
GET    /api/products/{id}/assembly-status
```

#### Real-Time Event API
```javascript
// SignalR client connection
connection.on("PartScanned", (partId, status) => {
    // Update UI with new status
});
```

---

## Architecture Decision Records

### ADR-001: Database Strategy
**Decision:** Use SQLite for development, SQL Server Express for production  
**Rationale:** Simplifies development setup while providing production performance  
**Alternatives:** Single database type, cloud database options  
**Status:** Approved

### ADR-002: UI Framework Choice
**Decision:** Razor Pages with server-side rendering  
**Rationale:** Shop floor terminals need simple, reliable interfaces  
**Alternatives:** Blazor, React/Angular SPA  
**Status:** Approved

### ADR-003: Real-Time Communication
**Decision:** SignalR for real-time updates  
**Rationale:** Native ASP.NET Core integration, WebSocket support  
**Alternatives:** Server-Sent Events, polling, third-party solutions  
**Status:** Approved

### ADR-004: Architecture Pattern
**Decision:** Clean Architecture with CQRS patterns  
**Rationale:** Maintainable, testable, supports future requirements  
**Alternatives:** N-Tier, Microservices, Simple MVC  
**Status:** Approved

---

## Future Architecture Considerations

### Scalability Enhancements
- **Microservices Migration:** Break into specialized services
- **Container Deployment:** Docker containerization for portability
- **Cloud Migration:** Azure or AWS hosting options
- **API Gateway:** Centralized API management

### Technology Upgrades
- **Progressive Web App (PWA):** Offline capability for terminals
- **Mobile Applications:** Native apps for warehouse personnel
- **IoT Integration:** RFID and sensor integration
- **Machine Learning:** Predictive analytics for optimization

### Security Enhancements
- **Certificate Authentication:** Enhanced security model
- **API Security:** OAuth 2.0 and JWT tokens
- **Audit Compliance:** Enhanced logging and compliance features
- **Network Security:** VPN and network isolation

---

*This architecture document serves as the blueprint for all development activities. All implementation decisions should align with the patterns and principles outlined in this document.*
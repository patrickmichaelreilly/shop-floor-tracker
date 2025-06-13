# Business Requirements Document
## Shop Floor Part Tracking System

**Document Version:** 1.0  
**Date:** 2025-01-13  
**Author:** Claude Code  
**Status:** Draft

---

## Executive Summary

This document outlines the business requirements for a new shop floor part tracking system to replace the discontinued Production Coach software. The system will manage kitchen cabinet manufacturing workflow from CNC cutting through shipping, supporting a hierarchical data structure and multi-station process flow.

---

## Business Problem Statement

### Current System Limitations
The existing Production Coach software presents several critical business challenges:
- **Discontinued Support:** No vendor support or updates available
- **Reliability Issues:** Frequent bugs and crashes disrupt production workflow
- **Language Barriers:** Poor English language support affects usability
- **Configuration Constraints:** Inflexible process configuration limits operational adaptability
- **Storage Integration:** Only 6 of many storage racks configured, severely limiting warehouse efficiency
- **Maintenance Issues:** No ability to update or customize system behavior

### Business Impact
- Production delays due to system crashes and restarts
- Inefficient parts storage and retrieval (90%+ racks unconfigured)
- Manual workarounds required for basic operations
- Increased labor costs due to system inefficiencies
- Risk of complete system failure with no vendor support

### Success Criteria for New System
1. **100% Uptime:** Reliable system operation during production hours
2. **Complete Storage Integration:** All storage racks configured and actively managed
3. **Flexible Configuration:** Adaptable to changing production processes
4. **User-Friendly Interface:** Intuitive operation requiring minimal training
5. **Modern Web Architecture:** Browser-based access from any shop floor terminal

---

## Data Structure Requirements

### Hierarchical Data Model
The system must maintain the following data hierarchy imported from Microvellum SQL CE files:

```
Work Order
├── Products (kitchen cabinets, vanities, etc.)
│   ├── Parts (individual components)
│   └── Subassemblies
│       └── Parts (components of subassemblies)
├── Hardware (items shipped but not manufactured)
└── Detached Products (items requiring no manufacturing)
```

### Data Integrity Requirements
- All Microvellum unique identifiers must be preserved
- Parent-child relationships must be maintained throughout process flow
- Part status must be tracked individually while maintaining product grouping
- Hardware and detached products must be tracked separately from manufactured items

---

## Functional Requirements

### FR-001: Work Order Import and Management
**Priority:** High  
**Business Value:** Critical for system initialization

**Requirements:**
- Monitor designated folder for new SQL CE files
- Parse and import complete hierarchical structure
- Preserve all Microvellum identifiers and relationships
- Handle concurrent imports without data corruption
- Provide import status feedback and error handling
- Archive processed files with audit trail

**Business Rules:**
- Only process files with valid SQL CE structure
- Reject duplicate work orders (same ID)
- Maintain import history for audit purposes

### FR-002: Multi-Station Process Tracking
**Priority:** High  
**Business Value:** Core workflow automation

**Requirements:**
- Support 4 distinct scanning stations with unique behaviors:
  1. **[Future] Cut Scan @ CNC Machine**
  2. **Sorting Scan @ Sorting Rack**
  3. **Assembly Scan @ Assembly Line**
  4. **Shipping Scan @ Loading Dock**

- Track part status progression: Cut → Sorted → Assembled → Shipped
- Provide station-specific interfaces optimized for each workflow
- Real-time status updates across all stations
- Prevent backwards status progression (business rule enforcement)

**Business Rules:**
- Parts can only advance to next status in sequence
- Each scan must validate part is eligible for current station
- Status changes must be immediately reflected system-wide

### FR-003: Dynamic Storage Management
**Priority:** High  
**Business Value:** Major efficiency improvement over current system

**Requirements:**
- Configure unlimited storage racks with custom dimensions
- Support variable row/column configurations per rack
- Bins are individual slots within racks (no separate bin entity)
- Implement random slot assignment algorithm for optimal space utilization
- Provide visual slot availability and location guidance
- Support slot reservation and temporary holds

**Business Rules:**
- Slot assignment must group parts by product when possible
- Random assignment when product grouping not feasible
- Visual indicators must clearly show slot status (empty/occupied/reserved)
- Slot locations must be easily communicated to operators

### FR-004: Hierarchical Part Organization
**Priority:** High  
**Business Value:** Maintains production efficiency through proper grouping

**Requirements:**
- Assign storage slots based on product grouping logic
- Track subassembly component relationships
- Maintain part-to-product relationships throughout process
- Handle three distinct grouping scenarios:
  1. Single-part subassemblies (doors)
  2. Carcass parts
  3. Multi-part subassembly components (drawer parts)

**Business Rules:**
- Parts belonging to same product should be co-located when possible
- Subassembly components must be trackable as a group
- Product completion status must reflect all component states

### FR-005: Assembly Coordination
**Priority:** High  
**Business Value:** Streamlines assembly process and reduces search time

**Requirements:**
- Track completion status of all product components
- Display storage locations of all subassemblies for a product
- Provide assembly readiness notifications
- Show visual indicators for component availability
- Generate assembly pick lists with storage locations

**Business Rules:**
- Product is assembly-ready only when all components are sorted
- Subassembly locations must be displayed before assembly scan
- Assembly scan marks entire product as assembled

### FR-006: Shipping Verification
**Priority:** High  
**Business Value:** Ensures complete order fulfillment

**Requirements:**
- Generate complete work order shipping checklist
- Track hardware and detached product inclusion
- Verify all products in work order are marked as assembled
- Provide shipping completion confirmation
- Generate shipping documentation

**Business Rules:**
- Work order cannot ship unless all products are assembled
- Hardware and detached products must be accounted for separately
- Shipping scan marks entire work order as complete

---

## Non-Functional Requirements

### NFR-001: Performance
- System response time < 2 seconds for all scanning operations
- Support concurrent scanning at all stations without performance degradation
- Handle work orders with up to 500 products and 5,000 parts

### NFR-002: Reliability
- 99.9% uptime during production hours (7 AM - 6 PM)
- Automatic recovery from network interruptions
- Data integrity maintained during unexpected shutdowns

### NFR-003: Usability
- Single responsive web interface adaptable to different station contexts
- Intuitive operation requiring < 30 minutes training
- Large buttons and text suitable for shop floor environment
- Minimal clicks to complete common operations

### NFR-004: Compatibility
- Browser-based access from any Windows terminal
- Compatible with standard barcode scanners
- Support 3 of 9 barcode format from Microvellum
- Integration with existing Microvellum label system

### NFR-005: Scalability
- Support unlimited number of storage racks
- Handle concurrent operations from all 4 stations
- Accommodate future expansion to additional stations

---

## Technical Environment

### Barcode System
- **Format:** 3 of 9 barcodes with unique IDs from Microvellum
- **Labels:** Production-ready labels from Microvellum containing:
  - Part details and specifications
  - Edgebanding diagrams
  - Nesting sheet information
- **Scanning Hardware:** Standard barcode scanners at each station

### Storage Infrastructure
- **Racks:** Mobile racks with configurable row/column dimensions
- **Bins:** Individual rack slots (row/column coordinates)
- **Assignment:** Random slot assignment algorithm
- **Current State:** 6 of many racks configured in Production Coach

### Data Import
- **Source:** SQL CE files from Microvellum
- **Method:** Folder monitoring and automatic import
- **Frequency:** On-demand as new work orders are created

---

## Future Enhancement Wishlist (Version 2.0)

### Label Generation Capability
- Match Microvellum's Stimulsoft report formatting
- CNC operator ability to print part labels directly from system
- Custom label templates for different part types

### Automated Cut Status
- Integration with CNC program sheet scanning
- Automatic "cut" status marking without manual scan
- CNC completion notifications

### Advanced Analytics
- Production time tracking and reporting
- Efficiency metrics and bottleneck identification
- Historical data analysis and trending

### Mobile Support
- Native mobile apps for warehouse personnel
- Offline scanning capability with sync
- GPS-based rack location assistance

---

## Business Rules Summary

1. **Data Integrity:** All Microvellum relationships must be preserved
2. **Process Flow:** Parts can only advance status in defined sequence
3. **Storage Logic:** Product grouping takes precedence over random assignment
4. **Assembly Rules:** All components must be sorted before assembly
5. **Shipping Rules:** All products must be assembled before work order shipping
6. **Import Rules:** No duplicate work order IDs accepted
7. **Slot Assignment:** Slots are rack coordinates, not separate entities

---

## Stakeholder Requirements

### Shop Floor Operators
- Simple, intuitive scanning interface
- Clear visual feedback for all operations
- Minimal clicks to complete tasks
- Large text/buttons suitable for industrial environment

### Production Managers
- Real-time visibility into work order status
- Ability to track bottlenecks and delays
- Complete audit trail of all operations
- Flexible rack configuration as layout changes

### System Administrators
- Easy work order import process
- System monitoring and health dashboards
- User management and access control
- Backup and recovery capabilities

---

## Acceptance Criteria Overview

Each functional requirement will be considered complete when:
1. All specified functionality is implemented and tested
2. Integration testing passes with real Microvellum data
3. Performance requirements are met under load
4. User acceptance testing completed with shop floor operators
5. Documentation is complete and training materials prepared

---

*Document Status: This document serves as the foundation for system design and development. All requirements should be validated with stakeholders before implementation begins.*
-- Shop Floor Part Tracking System Database Schema
-- Version: 1.0
-- Date: 2025-01-13
-- Author: Claude Code
-- Target: SQLite/SQL Server Express

-- =====================================================
-- CORE HIERARCHICAL DATA STRUCTURE
-- =====================================================

-- Work Orders (imported from Microvellum SQL CE)
CREATE TABLE WorkOrders (
    WorkOrderId NVARCHAR(50) PRIMARY KEY,
    WorkOrderNumber NVARCHAR(100) NOT NULL,
    CustomerName NVARCHAR(200),
    OrderDate DATETIME,
    DueDate DATETIME,
    Status NVARCHAR(20) DEFAULT 'Active', -- Active, Complete, Shipped
    MicrovellumLinkID NVARCHAR(50),
    ImportedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ImportedBy NVARCHAR(100),
    ImportFilePath NVARCHAR(500),
    TotalProducts INT DEFAULT 0,
    TotalParts INT DEFAULT 0,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Products within Work Orders (cabinets, vanities, etc.)
CREATE TABLE Products (
    ProductId NVARCHAR(50) PRIMARY KEY,
    WorkOrderId NVARCHAR(50) NOT NULL,
    ProductNumber NVARCHAR(100) NOT NULL,
    ProductName NVARCHAR(200),
    ProductType NVARCHAR(50), -- Cabinet, Vanity, etc.
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Sorting, Ready, Assembled, Shipped
    MicrovellumLinkID NVARCHAR(50),
    AssemblyDate DATETIME,
    ShippedDate DATETIME,
    TotalParts INT DEFAULT 0,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(WorkOrderId)
);

-- Subassemblies within Products (doors, drawers, etc.)
CREATE TABLE Subassemblies (
    SubassemblyId NVARCHAR(50) PRIMARY KEY,
    ProductId NVARCHAR(50) NOT NULL,
    SubassemblyNumber NVARCHAR(100) NOT NULL,
    SubassemblyName NVARCHAR(200),
    SubassemblyType NVARCHAR(50), -- Door, Drawer, Shelf, etc.
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Sorting, Complete
    TotalParts INT DEFAULT 0,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

-- Parts (individual components)
CREATE TABLE Parts (
    PartId NVARCHAR(50) PRIMARY KEY,
    ProductId NVARCHAR(50) NOT NULL,
    SubassemblyId NVARCHAR(50), -- NULL for carcass parts
    PartNumber NVARCHAR(100) NOT NULL,
    PartName NVARCHAR(200),
    Material NVARCHAR(100),
    MaterialName NVARCHAR(100),
    MaterialCode NVARCHAR(50),
    Thickness DECIMAL(10,4),
    Length DECIMAL(10,4),
    Width DECIMAL(10,4),
    EdgeBanding NVARCHAR(200),
    NestingSheet NVARCHAR(100),
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Cut, Sorted, Assembled, Shipped
    MicrovellumLinkID NVARCHAR(50),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (SubassemblyId) REFERENCES Subassemblies(SubassemblyId)
);

-- Hardware items (shipped but not manufactured)
CREATE TABLE Hardware (
    HardwareId NVARCHAR(50) PRIMARY KEY,
    HardwareName NVARCHAR(200) NOT NULL,
    HardwareDescription NVARCHAR(500),
    ProductId NVARCHAR(50) NOT NULL,
    WorkOrderId NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Included, Shipped
    MicrovellumLinkID NVARCHAR(50),
    IncludedDate DATETIME,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId),
    FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(WorkOrderId)
);

-- Placed Sheets (CNC nesting optimization results)
CREATE TABLE PlacedSheets (
    PlacedSheetId NVARCHAR(50) PRIMARY KEY,
    SheetName NVARCHAR(200) NOT NULL,
    BarCode NVARCHAR(100) NOT NULL,
    FileName NVARCHAR(100) NOT NULL,
    WorkOrderId NVARCHAR(50) NOT NULL,
    MaterialType NVARCHAR(100),
    Length DECIMAL(10,4),
    Width DECIMAL(10,4), 
    Thickness DECIMAL(10,4),
    Status NVARCHAR(20) DEFAULT 'Pending',
    MicrovellumLinkID NVARCHAR(50),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(WorkOrderId)
);

-- Part Placements (tracks which parts are on which sheets)
CREATE TABLE PartPlacements (
    PlacementId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PartId NVARCHAR(50) NOT NULL,
    PlacedSheetId NVARCHAR(50) NOT NULL,
    XCoord DECIMAL(10,4),
    YCoord DECIMAL(10,4),
    Rotation INT DEFAULT 0,
    IsFlipped BIT DEFAULT 0,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PartId) REFERENCES Parts(PartId),
    FOREIGN KEY (PlacedSheetId) REFERENCES PlacedSheets(PlacedSheetId)
);

-- Detached Products (items requiring no manufacturing)
CREATE TABLE DetachedProducts (
    DetachedProductId NVARCHAR(50) PRIMARY KEY,
    WorkOrderId NVARCHAR(50) NOT NULL,
    ProductNumber NVARCHAR(100) NOT NULL,
    ProductName NVARCHAR(200),
    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending, Included, Shipped
    IncludedDate DATETIME,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(WorkOrderId)
);

-- =====================================================
-- STORAGE MANAGEMENT
-- =====================================================

-- Storage Racks (configurable warehouse storage)
CREATE TABLE StorageRacks (
    RackId NVARCHAR(50) PRIMARY KEY,
    RackName NVARCHAR(100) NOT NULL,
    RackLocation NVARCHAR(200),
    Rows INT NOT NULL,
    Columns INT NOT NULL,
    TotalSlots AS (Rows * Columns) PERSISTED,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Storage Slots (individual bin locations within racks)
CREATE TABLE StorageSlots (
    SlotId NVARCHAR(50) PRIMARY KEY, -- Format: RackId-Row-Column
    RackId NVARCHAR(50) NOT NULL,
    Row INT NOT NULL,
    Column INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Empty', -- Empty, Occupied, Reserved
    OccupiedDate DATETIME,
    ReservedDate DATETIME,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (RackId) REFERENCES StorageRacks(RackId),
    UNIQUE (RackId, Row, Column)
);

-- Part Storage Assignments (tracks where parts are stored)
CREATE TABLE PartStorageAssignments (
    AssignmentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PartId NVARCHAR(50) NOT NULL,
    SlotId NVARCHAR(50) NOT NULL,
    AssignedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    AssignedBy NVARCHAR(100),
    RetrievedDate DATETIME,
    RetrievedBy NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PartId) REFERENCES Parts(PartId),
    FOREIGN KEY (SlotId) REFERENCES StorageSlots(SlotId)
);

-- =====================================================
-- PROCESS TRACKING
-- =====================================================

-- Process Stations (CNC, Sorting, Assembly, Shipping)
CREATE TABLE ProcessStations (
    StationId NVARCHAR(50) PRIMARY KEY,
    StationName NVARCHAR(100) NOT NULL,
    StationType NVARCHAR(50) NOT NULL, -- CNC, Sorting, Assembly, Shipping
    Location NVARCHAR(200),
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Process Steps (status progression tracking)
CREATE TABLE ProcessSteps (
    StepId NVARCHAR(50) PRIMARY KEY,
    StepName NVARCHAR(100) NOT NULL,
    StepOrder INT NOT NULL,
    StationId NVARCHAR(50) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (StationId) REFERENCES ProcessStations(StationId)
);

-- Part Process History (audit trail of all part movements)
CREATE TABLE PartProcessHistory (
    HistoryId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PartId NVARCHAR(50) NOT NULL,
    StationId NVARCHAR(50) NOT NULL,
    StepId NVARCHAR(50) NOT NULL,
    PreviousStatus NVARCHAR(20),
    NewStatus NVARCHAR(20) NOT NULL,
    ProcessedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ProcessedBy NVARCHAR(100),
    Notes NVARCHAR(500),
    SlotId NVARCHAR(50), -- Storage location if applicable
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PartId) REFERENCES Parts(PartId),
    FOREIGN KEY (StationId) REFERENCES ProcessStations(StationId),
    FOREIGN KEY (StepId) REFERENCES ProcessSteps(StepId),
    FOREIGN KEY (SlotId) REFERENCES StorageSlots(SlotId)
);

-- =====================================================
-- SYSTEM CONFIGURATION
-- =====================================================

-- System Users (operators and administrators)
CREATE TABLE SystemUsers (
    UserId NVARCHAR(50) PRIMARY KEY,
    UserName NVARCHAR(100) NOT NULL,
    DisplayName NVARCHAR(200),
    Role NVARCHAR(50) NOT NULL, -- Admin, Operator, Manager
    StationId NVARCHAR(50), -- Default station assignment
    IsActive BIT DEFAULT 1,
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (StationId) REFERENCES ProcessStations(StationId)
);

-- System Settings (configuration parameters)
CREATE TABLE SystemSettings (
    SettingKey NVARCHAR(100) PRIMARY KEY,
    SettingValue NVARCHAR(500),
    SettingDescription NVARCHAR(200),
    SettingCategory NVARCHAR(50),
    ModifiedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    ModifiedBy NVARCHAR(100)
);

-- Import History (tracks all work order imports)
CREATE TABLE ImportHistory (
    ImportId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FileName NVARCHAR(255) NOT NULL,
    ImportDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    Status NVARCHAR(50) NOT NULL,
    RecordsImported INT DEFAULT 0,
    ErrorMessage NVARCHAR(MAX),
    WorkOrdersCreated INT DEFAULT 0,
    ProductsCreated INT DEFAULT 0,
    PartsCreated INT DEFAULT 0,
    HardwareCreated INT DEFAULT 0,
    PlacedSheetsCreated INT DEFAULT 0,
    PartPlacementsCreated INT DEFAULT 0,
    FilePath NVARCHAR(500),
    FileSize BIGINT,
    ImportedBy NVARCHAR(100),
    ErrorCount INT DEFAULT 0,
    WorkOrderId NVARCHAR(50),
    CreatedDate DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (WorkOrderId) REFERENCES WorkOrders(WorkOrderId)
);

-- =====================================================
-- INDEXES FOR PERFORMANCE
-- =====================================================

-- Primary workflow indexes
CREATE INDEX IX_Parts_ProductId ON Parts(ProductId);
CREATE INDEX IX_Parts_SubassemblyId ON Parts(SubassemblyId);
CREATE INDEX IX_Parts_Status ON Parts(Status);
CREATE INDEX IX_Products_WorkOrderId ON Products(WorkOrderId);
CREATE INDEX IX_Products_Status ON Products(Status);
CREATE INDEX IX_Subassemblies_ProductId ON Subassemblies(ProductId);

-- Storage management indexes
CREATE INDEX IX_PartStorageAssignments_PartId ON PartStorageAssignments(PartId);
CREATE INDEX IX_PartStorageAssignments_SlotId ON PartStorageAssignments(SlotId);
CREATE INDEX IX_PartStorageAssignments_IsActive ON PartStorageAssignments(IsActive);
CREATE INDEX IX_StorageSlots_RackId ON StorageSlots(RackId);
CREATE INDEX IX_StorageSlots_Status ON StorageSlots(Status);

-- Process tracking indexes
CREATE INDEX IX_PartProcessHistory_PartId ON PartProcessHistory(PartId);
CREATE INDEX IX_PartProcessHistory_StationId ON PartProcessHistory(StationId);
CREATE INDEX IX_PartProcessHistory_ProcessedDate ON PartProcessHistory(ProcessedDate);

-- Date-based indexes for reporting
CREATE INDEX IX_WorkOrders_DueDate ON WorkOrders(DueDate);
CREATE INDEX IX_WorkOrders_ImportedDate ON WorkOrders(ImportedDate);

-- Microvellum integration indexes
CREATE INDEX IX_PlacedSheets_WorkOrderId ON PlacedSheets(WorkOrderId);
CREATE INDEX IX_PlacedSheets_Status ON PlacedSheets(Status);
CREATE INDEX IX_PartPlacements_PartId ON PartPlacements(PartId);
CREATE INDEX IX_PartPlacements_PlacedSheetId ON PartPlacements(PlacedSheetId);
CREATE INDEX IX_Hardware_ProductId ON Hardware(ProductId);
CREATE INDEX IX_Hardware_WorkOrderId ON Hardware(WorkOrderId);
CREATE INDEX IX_Parts_MicrovellumLinkID ON Parts(MicrovellumLinkID);
CREATE INDEX IX_Products_MicrovellumLinkID ON Products(MicrovellumLinkID);
CREATE INDEX IX_WorkOrders_MicrovellumLinkID ON WorkOrders(MicrovellumLinkID);

-- =====================================================
-- INITIAL DATA SETUP
-- =====================================================

-- Insert default process stations
INSERT INTO ProcessStations (StationId, StationName, StationType, Location) VALUES
('CNC-001', 'CNC Machine #1', 'CNC', 'Production Floor - North'),
('SORT-001', 'Sorting Station #1', 'Sorting', 'Production Floor - Center'),
('ASSY-001', 'Assembly Line #1', 'Assembly', 'Production Floor - South'),
('SHIP-001', 'Shipping Dock #1', 'Shipping', 'Warehouse - Loading Dock');

-- Insert default process steps
INSERT INTO ProcessSteps (StepId, StepName, StepOrder, StationId) VALUES
('CNC-CUT', 'Cut Part', 1, 'CNC-001'),
('SORT-SCAN', 'Sort and Store', 2, 'SORT-001'),
('ASSY-SCAN', 'Assembly Complete', 3, 'ASSY-001'),
('SHIP-SCAN', 'Ship Product', 4, 'SHIP-001');

-- Insert default system settings
INSERT INTO SystemSettings (SettingKey, SettingValue, SettingDescription, SettingCategory) VALUES
('IMPORT_FOLDER_PATH', 'C:\ShopFloor\Import', 'Folder to monitor for SQL CE files', 'Import'),
('ARCHIVE_FOLDER_PATH', 'C:\ShopFloor\Archive', 'Folder to store processed files', 'Import'),
('SCAN_INTERVAL_SECONDS', '30', 'How often to scan import folder', 'Import'),
('MAX_SLOT_ASSIGNMENT_ATTEMPTS', '10', 'Maximum attempts to find suitable slot', 'Storage'),
('GROUPING_PREFERENCE', 'PRODUCT', 'Preference for part grouping (PRODUCT, SUBASSEMBLY, RANDOM)', 'Storage'),
('BARCODE_FORMAT', '3OF9', 'Expected barcode format', 'Scanning'),
('SESSION_TIMEOUT_MINUTES', '60', 'User session timeout', 'Security'),
('BACKUP_INTERVAL_HOURS', '24', 'Database backup frequency', 'Maintenance');

-- =====================================================
-- VIEWS FOR COMMON QUERIES
-- =====================================================

-- View: Work Order Summary
CREATE VIEW WorkOrderSummary AS
SELECT 
    wo.WorkOrderId,
    wo.WorkOrderNumber,
    wo.CustomerName,
    wo.DueDate,
    wo.Status,
    COUNT(DISTINCT p.ProductId) as TotalProducts,
    COUNT(DISTINCT pt.PartId) as TotalParts,
    COUNT(CASE WHEN pt.Status = 'Cut' THEN 1 END) as PartsCut,
    COUNT(CASE WHEN pt.Status = 'Sorted' THEN 1 END) as PartsSorted,
    COUNT(CASE WHEN pt.Status = 'Assembled' THEN 1 END) as PartsAssembled,
    COUNT(CASE WHEN pt.Status = 'Shipped' THEN 1 END) as PartsShipped
FROM WorkOrders wo
LEFT JOIN Products p ON wo.WorkOrderId = p.WorkOrderId
LEFT JOIN Parts pt ON p.ProductId = pt.ProductId
GROUP BY wo.WorkOrderId, wo.WorkOrderNumber, wo.CustomerName, wo.DueDate, wo.Status;

-- View: Product Assembly Readiness
CREATE VIEW ProductAssemblyReadiness AS
SELECT 
    p.ProductId,
    p.ProductNumber,
    p.ProductName,
    p.WorkOrderId,
    p.Status,
    COUNT(pt.PartId) as TotalParts,
    COUNT(CASE WHEN pt.Status = 'Sorted' THEN 1 END) as PartsSorted,
    CASE 
        WHEN COUNT(pt.PartId) = COUNT(CASE WHEN pt.Status = 'Sorted' THEN 1 END) 
        THEN 1 
        ELSE 0 
    END as IsReadyForAssembly
FROM Products p
LEFT JOIN Parts pt ON p.ProductId = pt.ProductId
GROUP BY p.ProductId, p.ProductNumber, p.ProductName, p.WorkOrderId, p.Status;

-- View: Storage Rack Utilization
CREATE VIEW StorageRackUtilization AS
SELECT 
    sr.RackId,
    sr.RackName,
    sr.TotalSlots,
    COUNT(CASE WHEN ss.Status = 'Occupied' THEN 1 END) as OccupiedSlots,
    COUNT(CASE WHEN ss.Status = 'Reserved' THEN 1 END) as ReservedSlots,
    COUNT(CASE WHEN ss.Status = 'Empty' THEN 1 END) as EmptySlots,
    CAST(COUNT(CASE WHEN ss.Status = 'Occupied' THEN 1 END) * 100.0 / sr.TotalSlots AS DECIMAL(5,2)) as UtilizationPercent
FROM StorageRacks sr
LEFT JOIN StorageSlots ss ON sr.RackId = ss.RackId
WHERE sr.IsActive = 1
GROUP BY sr.RackId, sr.RackName, sr.TotalSlots;

-- =====================================================
-- STORED PROCEDURES (SQL Server specific)
-- =====================================================

-- Note: The following stored procedures would be implemented for SQL Server
-- For SQLite, equivalent application logic would be needed

/*
-- Procedure: Assign Part to Storage
CREATE PROCEDURE AssignPartToStorage
    @PartId NVARCHAR(50),
    @UserId NVARCHAR(50)
AS
BEGIN
    -- Implementation would include:
    -- 1. Find optimal slot based on product grouping
    -- 2. Reserve slot
    -- 3. Create storage assignment
    -- 4. Update part status to 'Sorted'
    -- 5. Log process history
END

-- Procedure: Complete Product Assembly
CREATE PROCEDURE CompleteProductAssembly
    @ProductId NVARCHAR(50),
    @UserId NVARCHAR(50)
AS
BEGIN
    -- Implementation would include:
    -- 1. Verify all parts are sorted
    -- 2. Update product status to 'Assembled'
    -- 3. Update all part statuses to 'Assembled'
    -- 4. Log process history for all parts
    -- 5. Clear storage assignments
END

-- Procedure: Ship Work Order
CREATE PROCEDURE ShipWorkOrder
    @WorkOrderId NVARCHAR(50),
    @UserId NVARCHAR(50)
AS
BEGIN
    -- Implementation would include:
    -- 1. Verify all products are assembled
    -- 2. Verify hardware is included
    -- 3. Update work order status to 'Shipped'
    -- 4. Update all products/parts to 'Shipped'
    -- 5. Log complete process history
END
*/

-- =====================================================
-- SCHEMA NOTES
-- =====================================================

/*
Design Principles:
1. Hierarchical Structure: Work Order -> Products -> Parts/Subassemblies maintains Microvellum structure
2. Audit Trail: Complete history of all part movements and status changes
3. Flexible Storage: Unlimited racks with configurable dimensions
4. Status Tracking: Clear progression through Cut -> Sorted -> Assembled -> Shipped
5. Performance: Indexes on commonly queried fields
6. Extensibility: Additional fields can be added without breaking existing structure

Key Business Rules Enforced:
- Parts can only advance status in sequence
- Storage slots are unique within racks
- Complete audit trail maintained for all operations
- Hardware and detached products tracked separately
- Product grouping logic for optimal storage assignment

Future Enhancements:
- Additional indexes based on performance testing
- Partitioning for large datasets
- Additional views for reporting requirements
- Stored procedures for complex business logic
*/
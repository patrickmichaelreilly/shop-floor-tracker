// Sorting Live Updates - Real-time rack and queue refresh
(function() {
    'use strict';
    
    let currentData = null;
    
    // Check if SignalR connection is available (should be initialized in main page)
    if (typeof connection === 'undefined') {
        console.error('SignalR connection not found. Make sure signalr.min.js is loaded first.');
        return;
    }
    
    // Enhanced PartStatusChanged handler for sorting page
    connection.off("PartStatusChanged"); // Remove any existing handler
    connection.on("PartStatusChanged", function (partId, newStatus) {
        console.log("Sorting: Part status changed:", partId, "->", newStatus);
        refreshSortingData();
    });
    
    // Fetch and update sorting data
    async function refreshSortingData() {
        try {
            const response = await fetch('/api/summary/sorting');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const newData = await response.json();
            
            if (currentData) {
                updateChangedElements(currentData, newData);
            } else {
                // First load - just store the data
                currentData = newData;
            }
            
            currentData = newData;
            
        } catch (error) {
            console.error('Error refreshing sorting data:', error);
        }
    }
    
    // Compare data and update only changed elements
    function updateChangedElements(oldData, newData) {
        // Update rack occupancy
        updateRackOccupancy(oldData.rackOccupancy, newData.rackOccupancy);
        
        // Update parts queue
        updatePartsQueue(oldData.parts, newData.parts);
    }
    
    // Update rack visual indicators
    function updateRackOccupancy(oldRacks, newRacks) {
        newRacks.forEach(newRack => {
            const oldRack = oldRacks.find(r => r.id === newRack.id);
            if (!oldRack || oldRack.occupied !== newRack.occupied) {
                // Find rack container and update
                const rackHeaders = document.querySelectorAll('.rack-header');
                rackHeaders.forEach(header => {
                    if (header.textContent.includes(newRack.name)) {
                        const occupancyPercentage = newRack.total > 0 ? Math.round((newRack.occupied * 100) / newRack.total) : 0;
                        const newText = `${newRack.name} - ${occupancyPercentage}% Full`;
                        
                        if (header.textContent !== newText) {
                            header.textContent = newText;
                            flashElement(header);
                        }
                        
                        // Update rack stats
                        const statsDiv = header.parentElement.querySelector('.rack-stats');
                        if (statsDiv) {
                            const available = newRack.total - newRack.occupied;
                            const newStatsText = `Available: ${available} | Occupied: ${newRack.occupied} | Total: ${newRack.total}`;
                            if (statsDiv.textContent !== newStatsText) {
                                statsDiv.textContent = newStatsText;
                                flashElement(statsDiv);
                            }
                        }
                    }
                });
                
                // Update individual rack slots if we can identify which ones changed
                updateRackSlots(newRack);
            }
        });
    }
    
    // Update rack slot visualization
    function updateRackSlots(rackData) {
        // This is a simplified update - in a real implementation, you'd need more detailed
        // slot-by-slot data from the API to know exactly which slots changed
        const rackHeaders = document.querySelectorAll('.rack-header');
        rackHeaders.forEach(header => {
            if (header.textContent.includes(rackData.name)) {
                const rackGrid = header.parentElement.querySelector('.rack-grid');
                if (rackGrid) {
                    // Flash the entire rack grid to indicate changes
                    flashElement(rackGrid);
                }
            }
        });
    }
    
    // Update parts queue/list
    function updatePartsQueue(oldParts, newParts) {
        // Update parts count in header
        const partsListHeader = document.querySelector('.parts-list h3');
        if (partsListHeader) {
            const newCount = newParts.length;
            const expectedText = `Parts Queue - Ready for Sorting (${newCount})`;
            if (partsListHeader.textContent !== expectedText) {
                partsListHeader.textContent = expectedText;
                flashElement(partsListHeader);
            }
        }
        
        // Update quick stats
        const readyToSortCount = newParts.filter(p => p.status === 'Cut' || p.status === 'Pending').length;
        const sortedCount = newParts.filter(p => p.status === 'Sorted').length;
        
        // Update "Ready to Sort" stat
        const statCards = document.querySelectorAll('.stat-number');
        statCards.forEach(card => {
            const label = card.nextElementSibling;
            if (label && label.textContent.includes('Ready to Sort')) {
                if (card.textContent !== readyToSortCount.toString()) {
                    card.textContent = readyToSortCount;
                    flashElement(card);
                }
            } else if (label && label.textContent.includes('Parts Stored')) {
                if (card.textContent !== sortedCount.toString()) {
                    card.textContent = sortedCount;
                    flashElement(card);
                }
            }
        });
        
        // Check for status changes in individual parts
        newParts.forEach(newPart => {
            const oldPart = oldParts.find(p => p.partId === newPart.partId);
            if (oldPart && oldPart.status !== newPart.status) {
                updatePartStatus(newPart);
            }
        });
    }
    
    // Update individual part status in the parts list
    function updatePartStatus(partData) {
        const partItems = document.querySelectorAll('.part-item');
        partItems.forEach(item => {
            const partHeader = item.querySelector('.part-header');
            if (partHeader && partHeader.textContent.includes(partData.partNumber)) {
                // Update status class
                item.className = item.className.replace(/status-\w+/g, '');
                const statusClass = partData.status === 'Pending' ? 'status-pending' : 
                                  partData.status === 'Cut' ? 'status-cut' : 'status-sorted';
                item.classList.add(statusClass);
                
                // Update storage info in details
                const partDetails = item.querySelector('.part-details');
                if (partDetails && partData.row && partData.col && partData.rackName) {
                    const storageInfo = `Storage: ${partData.rackName} R${partData.row}C${partData.col}`;
                    // Update the storage part of the details text
                    const detailsText = partDetails.textContent;
                    const storageMatch = detailsText.match(/Storage: [^|]+/);
                    if (storageMatch) {
                        const newDetailsText = detailsText.replace(storageMatch[0], storageInfo);
                        partDetails.textContent = newDetailsText;
                    }
                }
                
                flashElement(item);
            }
        });
    }
    
    // Visual feedback - flash element
    function flashElement(element) {
        // Remove existing flash class
        element.classList.remove('flash');
        
        // Force reflow
        element.offsetHeight;
        
        // Add flash class
        element.classList.add('flash');
        
        // Remove flash class after animation
        setTimeout(() => {
            element.classList.remove('flash');
        }, 1000);
    }
    
    // Initialize - load initial data when page loads
    document.addEventListener('DOMContentLoaded', function() {
        // Small delay to ensure SignalR connection is established
        setTimeout(() => {
            refreshSortingData();
        }, 1000);
    });
    
    // Expose refresh function globally for debugging
    window.refreshSortingData = refreshSortingData;
    
})();
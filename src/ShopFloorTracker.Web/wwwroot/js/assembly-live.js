// Assembly Live Updates - Real-time product and parts readiness refresh
(function() {
    'use strict';
    
    let currentData = null;
    
    // Check if SignalR connection is available (should be initialized in main page)
    if (typeof connection === 'undefined') {
        console.error('SignalR connection not found. Make sure signalr.min.js is loaded first.');
        return;
    }
    
    // Enhanced PartStatusChanged handler for assembly page
    connection.off("PartStatusChanged"); // Remove any existing handler
    connection.on("PartStatusChanged", function (partId, newStatus) {
        console.log("Assembly: Part status changed:", partId, "->", newStatus);
        refreshAssemblyData();
    });
    
    // Fetch and update assembly data
    async function refreshAssemblyData() {
        try {
            const response = await fetch('/api/summary/assembly');
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
            console.error('Error refreshing assembly data:', error);
        }
    }
    
    // Compare data and update only changed elements
    function updateChangedElements(oldData, newData) {
        // Update assembly stats
        updateAssemblyStats(oldData.products, newData.products);
        
        // Update product readiness indicators
        updateProductReadiness(oldData.products, newData.products);
        
        // Update component locations
        updateComponentLocations(oldData.products, newData.products);
    }
    
    // Update assembly statistics
    function updateAssemblyStats(oldProducts, newProducts) {
        const readyCount = newProducts.filter(p => p.isReady).length;
        const inProgressCount = newProducts.filter(p => p.sortedParts > 0 && !p.isReady).length;
        
        // Update "Ready to Assemble" stat
        const statCards = document.querySelectorAll('.stat-number');
        statCards.forEach(card => {
            const label = card.nextElementSibling;
            if (label && label.textContent.includes('Ready to Assemble')) {
                if (card.textContent !== readyCount.toString()) {
                    card.textContent = readyCount;
                    flashElement(card);
                }
            } else if (label && label.textContent.includes('In Progress')) {
                if (card.textContent !== inProgressCount.toString()) {
                    card.textContent = inProgressCount;
                    flashElement(card);
                }
            }
        });
        
        // Update main header count
        const productListHeader = document.querySelector('.product-list h3');
        if (productListHeader && productListHeader.textContent.includes('Products Ready for Assembly')) {
            const expectedText = `Products Ready for Assembly (${readyCount})`;
            if (productListHeader.textContent !== expectedText) {
                productListHeader.textContent = expectedText;
                flashElement(productListHeader);
            }
        }
    }
    
    // Update product readiness indicators and status
    function updateProductReadiness(oldProducts, newProducts) {
        newProducts.forEach(newProduct => {
            const oldProduct = oldProducts.find(p => p.productId === newProduct.productId);
            
            // Check if readiness status changed
            if (!oldProduct || oldProduct.isReady !== newProduct.isReady || 
                oldProduct.sortedParts !== newProduct.sortedParts ||
                oldProduct.assembledParts !== newProduct.assembledParts) {
                
                updateProductItem(newProduct, oldProduct);
            }
        });
    }
    
    // Update individual product item
    function updateProductItem(productData, oldProductData) {
        const productItems = document.querySelectorAll('.product-item');
        productItems.forEach(item => {
            const productHeader = item.querySelector('.product-header');
            if (productHeader && productHeader.textContent.includes(productData.productNumber)) {
                
                // Update readiness indicator
                const indicator = productHeader.querySelector('.ready-indicator, .in-progress-indicator');
                if (indicator) {
                    if (productData.isReady) {
                        indicator.className = 'ready-indicator';
                        indicator.textContent = 'READY FOR ASSEMBLY';
                    } else {
                        indicator.className = 'in-progress-indicator';
                        indicator.textContent = 'IN PROGRESS';
                    }
                }
                
                // Update progress percentage in details
                const productDetails = item.querySelector('.product-details');
                if (productDetails) {
                    const readyPercent = productData.totalParts > 0 ? 
                        Math.round((productData.sortedParts * 100) / productData.totalParts) : 0;
                    
                    // Update the parts ready information
                    const detailsText = productDetails.textContent;
                    const partsMatch = detailsText.match(/Parts Ready: \d+\/\d+ \(\d+%\)/);
                    if (partsMatch) {
                        const newPartsText = `Parts Ready: ${productData.sortedParts}/${productData.totalParts} (${readyPercent}%)`;
                        productDetails.textContent = detailsText.replace(partsMatch[0], newPartsText);
                    }
                }
                
                // Update part cards status
                updatePartCards(item, productData.parts);
                
                // If product became ready, flash the entire item
                if (oldProductData && !oldProductData.isReady && productData.isReady) {
                    flashElement(item);
                } else if (oldProductData && oldProductData.sortedParts !== productData.sortedParts) {
                    // If parts count changed, flash the product details
                    const productDetails = item.querySelector('.product-details');
                    if (productDetails) {
                        flashElement(productDetails);
                    }
                }
            }
        });
    }
    
    // Update individual part cards within a product
    function updatePartCards(productItem, partsData) {
        const partCards = productItem.querySelectorAll('.part-card');
        partCards.forEach(card => {
            const partNumberDiv = card.querySelector('div');
            if (partNumberDiv) {
                const partNumber = partNumberDiv.textContent;
                const partData = partsData.find(p => p.partNumber === partNumber);
                
                if (partData) {
                    // Update part status class
                    const newClass = partData.status === 'Sorted' ? 'part-sorted' : 
                                   partData.status === 'Assembled' ? 'part-assembled' : 'part-pending';
                    
                    if (!card.classList.contains(newClass)) {
                        // Remove existing status classes
                        card.classList.remove('part-sorted', 'part-pending', 'part-assembled');
                        card.classList.add(newClass);
                        
                        // Update location info
                        const locationDiv = card.lastElementChild;
                        if (locationDiv && partData.rackName && partData.row && partData.col) {
                            const newLocationText = `📍 ${partData.rackName} R${partData.row}C${partData.col}`;
                            if (locationDiv.textContent !== newLocationText) {
                                locationDiv.textContent = newLocationText;
                            }
                        }
                        
                        flashElement(card);
                    }
                }
            }
        });
    }
    
    // Update component locations sidebar
    function updateComponentLocations(oldProducts, newProducts) {
        // This would update the component locations section
        // For now, we'll just flash it if any products changed readiness
        const locationsSection = document.querySelector('.assembly-queue');
        if (locationsSection) {
            const hasReadinessChanges = newProducts.some(newProduct => {
                const oldProduct = oldProducts.find(p => p.productId === newProduct.productId);
                return !oldProduct || oldProduct.isReady !== newProduct.isReady;
            });
            
            if (hasReadinessChanges) {
                flashElement(locationsSection);
            }
        }
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
            refreshAssemblyData();
        }, 1000);
    });
    
    // Expose refresh function globally for debugging
    window.refreshAssemblyData = refreshAssemblyData;
    
})();
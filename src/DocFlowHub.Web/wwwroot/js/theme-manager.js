/**
 * DocFlowHub Theme Manager
 * Phase 8.1: Modern UI/UX Design System
 * Manages dynamic light/dark theme switching with user preference persistence
 */

class ThemeManager {
    constructor() {
        this.currentTheme = this.getStoredTheme() || this.getSystemPreference();
        this.themeToggleButton = null;
        this.initialized = false;
        
        // Theme change event listeners
        this.themeChangeListeners = [];
        
        console.log('ThemeManager: Initializing with theme:', this.currentTheme);
    }

    /**
     * Initialize the theme manager
     */
    init() {
        if (this.initialized) return;

        try {
            // Apply initial theme
            this.applyTheme(this.currentTheme, false);
            
            // Set up theme toggle button
            this.setupThemeToggle();
            
            // Listen for system theme changes
            this.setupSystemThemeListener();
            
            // Listen for storage changes (multiple tabs)
            this.setupStorageListener();
            
            this.initialized = true;
            console.log('ThemeManager: Initialized successfully');
            
        } catch (error) {
            console.error('ThemeManager: Failed to initialize:', error);
        }
    }

    /**
     * Get the user's stored theme preference
     */
    getStoredTheme() {
        try {
            // Check localStorage first
            const localTheme = localStorage.getItem('docflowhub-theme');
            if (localTheme && ['light', 'dark'].includes(localTheme)) {
                return localTheme;
            }
            
            // Check if user is authenticated and has a preference
            const userTheme = document.documentElement.getAttribute('data-user-theme');
            if (userTheme && ['light', 'dark'].includes(userTheme)) {
                return userTheme;
            }
            
            return null;
        } catch (error) {
            console.warn('ThemeManager: Error reading stored theme:', error);
            return null;
        }
    }

    /**
     * Get the system theme preference
     */
    getSystemPreference() {
        try {
            if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                return 'dark';
            }
            return 'light';
        } catch (error) {
            console.warn('ThemeManager: Error detecting system preference:', error);
            return 'light';
        }
    }

    /**
     * Apply a theme to the document
     */
    applyTheme(theme, animate = true) {
        try {
            // Validate theme
            if (!['light', 'dark'].includes(theme)) {
                console.warn('ThemeManager: Invalid theme:', theme);
                return;
            }

            // Add transition class for smooth animation
            if (animate) {
                document.documentElement.classList.add('theme-transitioning');
            }

            // Apply theme attribute
            document.documentElement.setAttribute('data-theme', theme);
            
            // Update current theme
            this.currentTheme = theme;
            
            // Update theme toggle button
            this.updateThemeToggleButton();
            
            // Store theme preference
            this.storeTheme(theme);
            
            // Notify listeners
            this.notifyThemeChange(theme);
            
            // Remove transition class after animation
            if (animate) {
                setTimeout(() => {
                    document.documentElement.classList.remove('theme-transitioning');
                }, 300);
            }
            
            console.log('ThemeManager: Applied theme:', theme);
            
        } catch (error) {
            console.error('ThemeManager: Error applying theme:', error);
        }
    }

    /**
     * Toggle between light and dark themes
     */
    toggle() {
        const newTheme = this.currentTheme === 'light' ? 'dark' : 'light';
        this.applyTheme(newTheme, true);
    }

    /**
     * Store theme preference
     */
    storeTheme(theme) {
        try {
            // Store in localStorage
            localStorage.setItem('docflowhub-theme', theme);
            
            // Send to server for authenticated users
            this.updateServerThemePreference(theme);
            
        } catch (error) {
            console.warn('ThemeManager: Error storing theme:', error);
        }
    }

    /**
     * Update server-side theme preference for authenticated users
     */
    async updateServerThemePreference(theme) {
        try {
            // Check if user is authenticated
            const isAuthenticated = document.body.classList.contains('authenticated') || 
                                   document.querySelector('[data-user-authenticated]');
            
            if (!isAuthenticated) return;

            // Send preference to server
            const response = await fetch('/api/profile/theme', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiforgeryToken()
                },
                body: JSON.stringify({ theme: theme })
            });

            if (!response.ok) {
                console.warn('ThemeManager: Failed to update server preference');
            }
            
        } catch (error) {
            console.warn('ThemeManager: Error updating server preference:', error);
        }
    }

    /**
     * Get antiforgery token for server requests
     */
    getAntiforgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }

    /**
     * Set up the theme toggle button
     */
    setupThemeToggle() {
        this.themeToggleButton = document.getElementById('themeToggle');
        
        if (this.themeToggleButton) {
            this.themeToggleButton.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggle();
            });
            
            // Add keyboard support
            this.themeToggleButton.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.toggle();
                }
            });
            
            this.updateThemeToggleButton();
        }
    }

    /**
     * Update theme toggle button appearance
     */
    updateThemeToggleButton() {
        if (!this.themeToggleButton) return;

        try {
            const lightIcon = this.themeToggleButton.querySelector('.theme-icon-light');
            const darkIcon = this.themeToggleButton.querySelector('.theme-icon-dark');
            
            if (lightIcon && darkIcon) {
                if (this.currentTheme === 'dark') {
                    lightIcon.style.opacity = '1';
                    darkIcon.style.opacity = '0';
                    this.themeToggleButton.setAttribute('aria-label', 'Switch to light theme');
                } else {
                    lightIcon.style.opacity = '0';
                    darkIcon.style.opacity = '1';
                    this.themeToggleButton.setAttribute('aria-label', 'Switch to dark theme');
                }
            }
            
        } catch (error) {
            console.warn('ThemeManager: Error updating toggle button:', error);
        }
    }

    /**
     * Listen for system theme changes
     */
    setupSystemThemeListener() {
        try {
            if (window.matchMedia) {
                const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
                
                mediaQuery.addEventListener('change', (e) => {
                    // Only auto-switch if user hasn't set an explicit preference
                    const storedTheme = localStorage.getItem('docflowhub-theme');
                    if (!storedTheme) {
                        const systemTheme = e.matches ? 'dark' : 'light';
                        this.applyTheme(systemTheme, true);
                    }
                });
            }
        } catch (error) {
            console.warn('ThemeManager: Error setting up system theme listener:', error);
        }
    }

    /**
     * Listen for storage changes (multiple tabs)
     */
    setupStorageListener() {
        try {
            window.addEventListener('storage', (e) => {
                if (e.key === 'docflowhub-theme' && e.newValue) {
                    const newTheme = e.newValue;
                    if (['light', 'dark'].includes(newTheme) && newTheme !== this.currentTheme) {
                        this.applyTheme(newTheme, true);
                    }
                }
            });
        } catch (error) {
            console.warn('ThemeManager: Error setting up storage listener:', error);
        }
    }

    /**
     * Add theme change listener
     */
    addThemeChangeListener(callback) {
        if (typeof callback === 'function') {
            this.themeChangeListeners.push(callback);
        }
    }

    /**
     * Remove theme change listener
     */
    removeThemeChangeListener(callback) {
        const index = this.themeChangeListeners.indexOf(callback);
        if (index > -1) {
            this.themeChangeListeners.splice(index, 1);
        }
    }

    /**
     * Notify all theme change listeners
     */
    notifyThemeChange(theme) {
        this.themeChangeListeners.forEach(callback => {
            try {
                callback(theme);
            } catch (error) {
                console.warn('ThemeManager: Error in theme change listener:', error);
            }
        });
    }

    /**
     * Get current theme
     */
    getCurrentTheme() {
        return this.currentTheme;
    }

    /**
     * Set theme programmatically
     */
    setTheme(theme) {
        this.applyTheme(theme, true);
    }
}

// Create global instance
const themeManager = new ThemeManager();

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        themeManager.init();
    });
} else {
    themeManager.init();
}

// Make it globally available
window.ThemeManager = themeManager;

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ThemeManager;
} 
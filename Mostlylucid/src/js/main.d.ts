interface GlobalState {
    isMobileMenuOpen: boolean;
    isDarkMode: boolean;
    themeInit(): void;
    themeSwitch(): void;
}
declare function global(): GlobalState;

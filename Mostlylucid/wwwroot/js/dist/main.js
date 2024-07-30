/*
 * ATTENTION: An "eval-source-map" devtool has been used.
 * This devtool is neither made for production nor for readable output files.
 * It uses "eval()" calls to create a separate source file with attached SourceMaps in the browser devtools.
 * If you are trying to read the output file, select a different devtool (https://webpack.js.org/configuration/devtool/)
 * or disable the default devtool with "devtool: false".
 * If you are looking for production-ready output files, see mode: "production" (https://webpack.js.org/configuration/mode/).
 */
var Mostlylucid;
/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ "./src/js/main.ts":
/*!************************!*\
  !*** ./src/js/main.ts ***!
  \************************/
/***/ (() => {

eval("\nfunction main() {\n    return {\n        isMobileMenuOpen: false,\n        isDarkMode: false,\n        themeInit() {\n            if (localStorage.theme === \"dark\" ||\n                (!(\"theme\" in localStorage) &&\n                    window.matchMedia(\"(prefers-color-scheme: dark)\").matches)) {\n                localStorage.theme = \"dark\";\n                document.documentElement.classList.add(\"dark\");\n                this.isDarkMode = true;\n            }\n            else {\n                localStorage.theme = \"light\";\n                document.documentElement.classList.remove(\"dark\");\n                this.isDarkMode = false;\n            }\n        },\n        themeSwitch() {\n            if (localStorage.theme === \"dark\") {\n                localStorage.theme = \"light\";\n                document.documentElement.classList.remove(\"dark\");\n                this.isDarkMode = false;\n            }\n            else {\n                localStorage.theme = \"dark\";\n                document.documentElement.classList.add(\"dark\");\n                this.isDarkMode = true;\n            }\n        },\n    };\n}\n//# sourceURL=[module]\n//# sourceMappingURL=data:application/json;charset=utf-8;base64,eyJ2ZXJzaW9uIjozLCJmaWxlIjoiLi9zcmMvanMvbWFpbi50cyIsIm1hcHBpbmdzIjoiO0FBT0EsU0FBUyxJQUFJO0lBQ1QsT0FBTztRQUNILGdCQUFnQixFQUFFLEtBQUs7UUFDdkIsVUFBVSxFQUFFLEtBQUs7UUFDakIsU0FBUztZQUNMLElBQ0ksWUFBWSxDQUFDLEtBQUssS0FBSyxNQUFNO2dCQUM3QixDQUFDLENBQUMsQ0FBQyxPQUFPLElBQUksWUFBWSxDQUFDO29CQUN2QixNQUFNLENBQUMsVUFBVSxDQUFDLDhCQUE4QixDQUFDLENBQUMsT0FBTyxDQUFDLEVBQ2hFLENBQUM7Z0JBQ0MsWUFBWSxDQUFDLEtBQUssR0FBRyxNQUFNLENBQUM7Z0JBQzVCLFFBQVEsQ0FBQyxlQUFlLENBQUMsU0FBUyxDQUFDLEdBQUcsQ0FBQyxNQUFNLENBQUMsQ0FBQztnQkFDL0MsSUFBSSxDQUFDLFVBQVUsR0FBRyxJQUFJLENBQUM7WUFDM0IsQ0FBQztpQkFBTSxDQUFDO2dCQUNKLFlBQVksQ0FBQyxLQUFLLEdBQUcsT0FBTyxDQUFDO2dCQUM3QixRQUFRLENBQUMsZUFBZSxDQUFDLFNBQVMsQ0FBQyxNQUFNLENBQUMsTUFBTSxDQUFDLENBQUM7Z0JBQ2xELElBQUksQ0FBQyxVQUFVLEdBQUcsS0FBSyxDQUFDO1lBQzVCLENBQUM7UUFDTCxDQUFDO1FBQ0QsV0FBVztZQUNQLElBQUksWUFBWSxDQUFDLEtBQUssS0FBSyxNQUFNLEVBQUUsQ0FBQztnQkFDaEMsWUFBWSxDQUFDLEtBQUssR0FBRyxPQUFPLENBQUM7Z0JBQzdCLFFBQVEsQ0FBQyxlQUFlLENBQUMsU0FBUyxDQUFDLE1BQU0sQ0FBQyxNQUFNLENBQUMsQ0FBQztnQkFDbEQsSUFBSSxDQUFDLFVBQVUsR0FBRyxLQUFLLENBQUM7WUFDNUIsQ0FBQztpQkFBTSxDQUFDO2dCQUNKLFlBQVksQ0FBQyxLQUFLLEdBQUcsTUFBTSxDQUFDO2dCQUM1QixRQUFRLENBQUMsZUFBZSxDQUFDLFNBQVMsQ0FBQyxHQUFHLENBQUMsTUFBTSxDQUFDLENBQUM7Z0JBQy9DLElBQUksQ0FBQyxVQUFVLEdBQUcsSUFBSSxDQUFDO1lBQzNCLENBQUM7UUFDTCxDQUFDO0tBQ0osQ0FBQztBQUNOLENBQUMiLCJzb3VyY2VzIjpbIndlYnBhY2s6Ly9Nb3N0bHlsdWNpZC8uL3NyYy9qcy9tYWluLnRzP2Q5NGEiXSwic291cmNlc0NvbnRlbnQiOlsiaW50ZXJmYWNlIEdsb2JhbFN0YXRlIHtcclxuICAgIGlzTW9iaWxlTWVudU9wZW46IGJvb2xlYW47XHJcbiAgICBpc0RhcmtNb2RlOiBib29sZWFuO1xyXG4gICAgdGhlbWVJbml0KCk6IHZvaWQ7XHJcbiAgICB0aGVtZVN3aXRjaCgpOiB2b2lkO1xyXG59XHJcblxyXG5mdW5jdGlvbiBtYWluKCk6IEdsb2JhbFN0YXRlIHtcclxuICAgIHJldHVybiB7XHJcbiAgICAgICAgaXNNb2JpbGVNZW51T3BlbjogZmFsc2UsXHJcbiAgICAgICAgaXNEYXJrTW9kZTogZmFsc2UsXHJcbiAgICAgICAgdGhlbWVJbml0KCkge1xyXG4gICAgICAgICAgICBpZiAoXHJcbiAgICAgICAgICAgICAgICBsb2NhbFN0b3JhZ2UudGhlbWUgPT09IFwiZGFya1wiIHx8XHJcbiAgICAgICAgICAgICAgICAoIShcInRoZW1lXCIgaW4gbG9jYWxTdG9yYWdlKSAmJlxyXG4gICAgICAgICAgICAgICAgICAgIHdpbmRvdy5tYXRjaE1lZGlhKFwiKHByZWZlcnMtY29sb3Itc2NoZW1lOiBkYXJrKVwiKS5tYXRjaGVzKVxyXG4gICAgICAgICAgICApIHtcclxuICAgICAgICAgICAgICAgIGxvY2FsU3RvcmFnZS50aGVtZSA9IFwiZGFya1wiO1xyXG4gICAgICAgICAgICAgICAgZG9jdW1lbnQuZG9jdW1lbnRFbGVtZW50LmNsYXNzTGlzdC5hZGQoXCJkYXJrXCIpO1xyXG4gICAgICAgICAgICAgICAgdGhpcy5pc0RhcmtNb2RlID0gdHJ1ZTtcclxuICAgICAgICAgICAgfSBlbHNlIHtcclxuICAgICAgICAgICAgICAgIGxvY2FsU3RvcmFnZS50aGVtZSA9IFwibGlnaHRcIjtcclxuICAgICAgICAgICAgICAgIGRvY3VtZW50LmRvY3VtZW50RWxlbWVudC5jbGFzc0xpc3QucmVtb3ZlKFwiZGFya1wiKTtcclxuICAgICAgICAgICAgICAgIHRoaXMuaXNEYXJrTW9kZSA9IGZhbHNlO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSxcclxuICAgICAgICB0aGVtZVN3aXRjaCgpIHtcclxuICAgICAgICAgICAgaWYgKGxvY2FsU3RvcmFnZS50aGVtZSA9PT0gXCJkYXJrXCIpIHtcclxuICAgICAgICAgICAgICAgIGxvY2FsU3RvcmFnZS50aGVtZSA9IFwibGlnaHRcIjtcclxuICAgICAgICAgICAgICAgIGRvY3VtZW50LmRvY3VtZW50RWxlbWVudC5jbGFzc0xpc3QucmVtb3ZlKFwiZGFya1wiKTtcclxuICAgICAgICAgICAgICAgIHRoaXMuaXNEYXJrTW9kZSA9IGZhbHNlO1xyXG4gICAgICAgICAgICB9IGVsc2Uge1xyXG4gICAgICAgICAgICAgICAgbG9jYWxTdG9yYWdlLnRoZW1lID0gXCJkYXJrXCI7XHJcbiAgICAgICAgICAgICAgICBkb2N1bWVudC5kb2N1bWVudEVsZW1lbnQuY2xhc3NMaXN0LmFkZChcImRhcmtcIik7XHJcbiAgICAgICAgICAgICAgICB0aGlzLmlzRGFya01vZGUgPSB0cnVlO1xyXG4gICAgICAgICAgICB9XHJcbiAgICAgICAgfSxcclxuICAgIH07XHJcbn0iXSwibmFtZXMiOltdLCJzb3VyY2VSb290IjoiIn0=\n//# sourceURL=webpack-internal:///./src/js/main.ts\n");

/***/ })

/******/ 	});
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module can't be inlined because the eval-source-map devtool is used.
/******/ 	var __webpack_exports__ = {};
/******/ 	__webpack_modules__["./src/js/main.ts"]();
/******/ 	Mostlylucid = __webpack_exports__;
/******/ 	
/******/ })()
;
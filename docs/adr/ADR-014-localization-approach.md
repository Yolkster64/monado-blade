# ADR-014: Localization and Internationalization Support

## Status
Accepted

## Context
Monado Blade may be deployed globally and needs to support multiple languages.

## Problem
How to support multiple languages while keeping code maintainable?

## Decision
Implement localization through resource files and runtime selection:
1. Store strings in resource files organized by language
2. Detect system language at startup
3. Support explicit language selection
4. Use placeholder-based formatting for parameterized strings
5. Support date/time/number localization
6. Provide fallback to English if language not available
7. Support adding new languages without code changes

## Consequences

### Positive
- Easy to support multiple languages
- Strings centralized and maintainable
- Translators can work on resource files
- Minimal code changes for new languages

### Negative
- Need to maintain translations
- Translation quality varies
- Strings duplicated across languages
- Complex formatting for plural rules

## Alternatives Considered

1. **Hard-coded English**: Simpler but not international
2. **External Translation Service**: More dynamic but requires service dependency

## Implementation Details
- Resource files in Resources/ directory
- One subdirectory per language (en, es, fr, de, etc.)
- Strings organized by category (UI, Errors, Messages)
- LocalizationService for runtime string resolution
- String IDs using dot notation (e.g., "errors.usb.notfound")

## Supported Languages (Phase 2)
- English (en)
- Spanish (es)
- French (fr)
- German (de)
- Chinese (zh-CN)
- Japanese (ja)

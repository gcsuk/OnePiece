# OnePiece Card Library

A Blazor application that uses OpenAI's Vision API to analyze OnePiece trading cards and build a comprehensive library.

## Features

- **AI-Powered Card Analysis**: Uses GPT-4 Vision to extract detailed information from OnePiece card images
- **Structured Data Extraction**: Captures card name, character, type, rarity, power, cost, attributes, and more
- **Library Management**: Save analyzed cards to a local library
- **JSON Export**: Export card data in structured JSON format
- **Japanese Text Recognition**: Automatically reads and extracts Japanese text from cards
- **English Translation**: Provides English translations when possible

## Setup

### 1. OpenAI API Key

You'll need an OpenAI API key with access to GPT-4 Vision. Add it to your user secrets:

```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
```

Or add it to `appsettings.Development.json`:

```json
{
  "OpenAI": {
    "ApiKey": "your-api-key-here",
    "Model": "gpt-4-vision-preview",
    "MaxTokens": 1000,
    "Temperature": 0.1
  }
}
```

### 2. Configuration Options

- **Model**: The OpenAI model to use (default: gpt-4-vision-preview)
- **MaxTokens**: Maximum tokens for the response (default: 1000)
- **Temperature**: Response creativity (default: 0.1 for consistent results)

## Usage

1. **Upload Card Image**: Select a JPG image of a OnePiece trading card
2. **Analyze Card**: Click "Analyze OnePiece Card" to process the image with AI
3. **Review Results**: View the extracted card information including:
   - Card name and character
   - Type, rarity, power, cost
   - Attributes and set information
   - Japanese card text
   - English translations
   - Artwork descriptions
4. **Save to Library**: Store the card data locally
5. **Export JSON**: Download the card data as a structured JSON file

## Card Data Structure

The system extracts the following information:

```json
{
  "cardName": "Official card name",
  "characterName": "Character featured on the card",
  "cardType": "Type of card (Character, Event, Stage, etc.)",
  "rarity": "Card rarity (Common, Rare, Super Rare, etc.)",
  "power": "Power/attack value",
  "cost": "Cost to play",
  "attribute": "Card attribute (Red, Blue, Green, etc.)",
  "cardNumber": "Card number in set",
  "setName": "Card set/expansion name",
  "cardText": "Full card text in Japanese",
  "cardTextEnglish": "English translation of card text",
  "artworkDescription": "Description of the artwork",
  "series": "OnePiece series/arc",
  "confidence": 0.95
}
```

## Technical Details

- **Framework**: .NET 9 Blazor Server
- **AI Service**: OpenAI GPT-4 Vision API
- **Data Storage**: Local JSON files in `wwwroot/library/`
- **Image Support**: JPG/JPEG format, up to 10MB
- **Response Format**: Structured JSON with camelCase properties

## File Structure

```
wwwroot/
├── uploads/          # Temporary uploaded images
└── library/          # Saved card data (JSON files)
```

## Error Handling

The system includes comprehensive error handling for:
- API failures
- Invalid image formats
- Network issues
- JSON parsing errors

## Future Enhancements

- Database integration for better search and organization
- Card image storage and management
- Advanced filtering and search capabilities
- Card value tracking over time
- Integration with trading card databases
- Bulk import/export functionality

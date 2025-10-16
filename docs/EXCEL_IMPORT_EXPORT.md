# Excel Import/Export for MCP Adapters

This document describes how to use the Excel import/export functionality for MCP adapters.

## Export Adapters

### How to Export
1. Go to the Dashboard
2. Click the **Export** button
3. An Excel file will be downloaded with all your adapters

### Export File Structure

The exported Excel file contains the following columns:

| Column | Type | Description | Example |
|--------|------|-------------|---------|
| **Name** | String | Unique adapter name | `my-adapter` |
| **Description** | String | Adapter description (supports markdown) | `This adapter connects to...` |
| **Type** | Enum | Adapter type | `StreamableHttp` or `Sse` |
| **Url** | String | Adapter URL | `http://localhost:3000` |
| **Enabled** | Boolean | Whether adapter is enabled | `True` or `False` |
| **Timeout (seconds)** | Number | Request timeout in seconds | `30` |
| **Headers (JSON)** | JSON String | Custom HTTP headers | `{"Authorization": "Bearer token"}` |
| **Created At** | DateTime | Creation timestamp | `2025-01-15 10:30:00` |
| **Updated At** | DateTime | Last update timestamp | `2025-01-15 10:30:00` |
| **Status** | String | Current adapter status | `healthy`, `unhealthy`, `unknown` |

---

## Import Adapters

### How to Import
1. Go to the Dashboard
2. Click the **Import** button
3. Select an Excel (.xlsx) file
4. Review the import results

### Import File Structure

For importing, you only need to provide these **required columns**:

| Column | Required | Type | Description | Example |
|--------|----------|------|-------------|---------|
| **Name** | ✅ Yes | String | Unique adapter name | `my-adapter` |
| **Description** | No | String | Adapter description | `This adapter...` |
| **Type** | ✅ Yes | Enum | Adapter type | `StreamableHttp` or `Sse` |
| **Url** | ✅ Yes | String | Adapter URL | `http://localhost:3000` |
| **Enabled** | No | Boolean | Whether adapter is enabled (default: `true`) | `True`, `False`, `1`, `0`, `yes`, `no` |
| **Timeout (seconds)** | No | Number | Request timeout (default: `30`) | `30` |
| **Headers (JSON)** | No | JSON String | Custom HTTP headers | `{"key": "value"}` |

**Note:** Columns beyond index 6 (Created At, Updated At, Status) are ignored during import as they are system-generated.

### Adapter Types

Valid values for the **Type** column:
- `StreamableHttp` - For HTTP/SSE streaming adapters
- `Sse` - For Server-Sent Events adapters

### Boolean Values

The **Enabled** column accepts various formats:
- `true`, `True`, `TRUE`
- `false`, `False`, `FALSE`
- `1` (true), `0` (false)
- `yes`, `Yes`, `YES` (true)
- `no`, `No`, `NO` (false)

### Headers Format

The **Headers (JSON)** column must contain valid JSON:

**Valid:**
```json
{"Authorization": "Bearer token123", "X-API-Key": "abc"}
```

**Invalid:**
```
Authorization: Bearer token123
```

---

## Import Validation

### Validation Rules

1. **Name** - Must not be empty and must be unique
2. **URL** - Must be a valid absolute URL (e.g., `http://` or `https://`)
3. **Type** - Must be either `StreamableHttp` or `Sse`
4. **Headers** - If provided, must be valid JSON

### Import Results

After import, you'll see a summary:
- **Success Count**: Number of adapters successfully imported
- **Failed Count**: Number of adapters that failed to import
- **Validation Errors**: List of validation warnings
- **Failed Adapters**: List of adapters that failed with reasons

### Example Import Messages

```
Import completed. 5 adapters imported successfully.

Validation warnings:
Row 3: Invalid adapter type 'Http'. Using default 'Sse'.
Row 7: Invalid JSON format for headers. Using empty headers.

Failed adapters:
- duplicate-adapter: Adapter with name 'duplicate-adapter' already exists
```

---

## Best Practices

### For Export
1. Export regularly as backup
2. Keep exported files for audit trail
3. Use exported files as templates for bulk imports

### For Import
1. Always validate the Excel file before importing
2. Test with a small batch first
3. Check adapter names don't conflict with existing ones
4. Ensure URLs are correct and reachable
5. Use proper JSON format for headers
6. Review validation errors after import

---

## Example Excel Template

You can create a new Excel file with this structure:

| Name | Description | Type | Url | Enabled | Timeout (seconds) | Headers (JSON) |
|------|-------------|------|-----|---------|-------------------|----------------|
| test-adapter | Test adapter for demo | StreamableHttp | http://localhost:3000 | True | 30 | {"X-API-Key": "test"} |
| sse-adapter | SSE streaming adapter | Sse | http://localhost:4000/sse | True | 60 | {} |

---

## Troubleshooting

### "No file uploaded"
- Make sure you selected a file before clicking Import

### "File must be an Excel (.xlsx) file"
- Only `.xlsx` files are supported (not `.xls` or `.csv`)

### "Row X: Name is required"
- Ensure the Name column is filled for all rows

### "Row X: Invalid URL format"
- URLs must be absolute (include `http://` or `https://`)

### "Adapter with name 'X' already exists"
- Adapter names must be unique
- Either delete the existing adapter first or use a different name

### "Invalid JSON format for headers"
- Check that your JSON is properly formatted
- Use double quotes for keys and values
- Ensure no trailing commas

---

## API Endpoints

### Export
```
GET /api/adapters/export
Returns: Excel file (application/vnd.openxmlformats-officedocument.spreadsheetml.sheet)
```

### Import
```
POST /api/adapters/import
Content-Type: multipart/form-data
Body: file (Excel .xlsx file)
Returns: {
  message: string,
  successCount: number,
  failedCount: number,
  validationErrors: string[],
  failedAdapters: Array<{name: string, error: string}>
}
```


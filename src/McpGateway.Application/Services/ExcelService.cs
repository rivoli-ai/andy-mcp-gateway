using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using McpGateway.Application.DTOs;
using McpGateway.Domain.Enums;
using System.Text.Json;

namespace McpGateway.Application.Services;

/// <summary>
/// Service for exporting and importing MCP adapters to/from Excel files.
/// </summary>
public class ExcelService
{
    /// <summary>
    /// Exports adapters to an Excel file.
    /// </summary>
    /// <param name="adapters">List of adapters to export</param>
    /// <returns>Excel file as byte array</returns>
    public byte[] ExportAdaptersToExcel(IEnumerable<McpAdapterDto> adapters)
    {
        using var memoryStream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            // Add a WorkbookPart
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            // Add a WorksheetPart
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            var sheet = new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Adapters"
            };
            sheets.Append(sheet);

            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>()!;

            // Add header row
            var headerRow = new Row { RowIndex = 1 };
            headerRow.Append(
                CreateCell("Name", CellValues.String),
                CreateCell("Description", CellValues.String),
                CreateCell("Type", CellValues.String),
                CreateCell("Url", CellValues.String),
                CreateCell("Enabled", CellValues.String),
                CreateCell("Timeout (seconds)", CellValues.String),
                CreateCell("Headers (JSON)", CellValues.String),
                CreateCell("Created At", CellValues.String),
                CreateCell("Updated At", CellValues.String),
                CreateCell("Status", CellValues.String)
            );
            sheetData.Append(headerRow);

            // Add data rows
            uint rowIndex = 2;
            foreach (var adapter in adapters)
            {
                var dataRow = new Row { RowIndex = rowIndex };
                dataRow.Append(
                    CreateCell(adapter.Name, CellValues.String),
                    CreateCell(adapter.Description ?? "", CellValues.String),
                    CreateCell(adapter.Type.ToString(), CellValues.String),
                    CreateCell(adapter.Url, CellValues.String),
                    CreateCell(adapter.Enabled.ToString(), CellValues.String),
                    CreateCell(adapter.TimeoutSeconds.ToString(), CellValues.Number),
                    CreateCell(adapter.Headers != null ? JsonSerializer.Serialize(adapter.Headers) : "", CellValues.String),
                    CreateCell(adapter.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"), CellValues.String),
                    CreateCell(adapter.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"), CellValues.String),
                    CreateCell(adapter.Status, CellValues.String)
                );
                sheetData.Append(dataRow);
                rowIndex++;
            }

            workbookPart.Workbook.Save();
        }

        return memoryStream.ToArray();
    }

    /// <summary>
    /// Imports adapters from an Excel file.
    /// </summary>
    /// <param name="fileStream">Excel file stream</param>
    /// <returns>List of create adapter DTOs with validation results</returns>
    public (List<CreateMcpAdapterDto> Adapters, List<string> Errors) ImportAdaptersFromExcel(Stream fileStream)
    {
        var adapters = new List<CreateMcpAdapterDto>();
        var errors = new List<string>();

        try
        {
            using var document = SpreadsheetDocument.Open(fileStream, false);
            var workbookPart = document.WorkbookPart;
            if (workbookPart == null)
            {
                errors.Add("Invalid Excel file: No workbook found.");
                return (adapters, errors);
            }

            var sheets = workbookPart.Workbook.Descendants<Sheet>();
            var firstSheet = sheets.FirstOrDefault();
            if (firstSheet == null)
            {
                errors.Add("No sheets found in the Excel file.");
                return (adapters, errors);
            }

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(firstSheet.Id!);
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null)
            {
                errors.Add("No data found in the sheet.");
                return (adapters, errors);
            }

            var rows = sheetData.Elements<Row>().ToList();
            if (rows.Count < 2)
            {
                errors.Add("File must contain at least a header row and one data row.");
                return (adapters, errors);
            }

            // Skip header row (index 0) and process data rows
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                var rowNumber = i + 1;
                
                try
                {
                    var cells = row.Elements<Cell>().ToList();
                    if (cells.Count < 4) // At least Name, Type, Url required
                    {
                        errors.Add($"Row {rowNumber}: Insufficient data (minimum: Name, Description, Type, Url).");
                        continue;
                    }

                    var adapter = new CreateMcpAdapterDto
                    {
                        Name = GetCellValue(workbookPart, cells[0]),
                        Description = GetCellValue(workbookPart, cells[1]),
                        Type = ParseAdapterType(GetCellValue(workbookPart, cells[2]), rowNumber, errors),
                        Url = GetCellValue(workbookPart, cells[3]),
                        Enabled = cells.Count > 4 ? ParseBool(GetCellValue(workbookPart, cells[4]), true) : true,
                        TimeoutSeconds = cells.Count > 5 ? ParseInt(GetCellValue(workbookPart, cells[5]), 30) : 30,
                        Headers = cells.Count > 6 ? ParseHeaders(GetCellValue(workbookPart, cells[6]), rowNumber, errors) : new Dictionary<string, string>()
                    };

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(adapter.Name))
                    {
                        errors.Add($"Row {rowNumber}: Name is required.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(adapter.Url))
                    {
                        errors.Add($"Row {rowNumber}: URL is required.");
                        continue;
                    }

                    if (!Uri.TryCreate(adapter.Url, UriKind.Absolute, out _))
                    {
                        errors.Add($"Row {rowNumber}: Invalid URL format.");
                        continue;
                    }

                    adapters.Add(adapter);
                }
                catch (Exception ex)
                {
                    errors.Add($"Row {rowNumber}: Error processing row - {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error reading Excel file: {ex.Message}");
        }

        return (adapters, errors);
    }

    /// <summary>
    /// Creates a cell with specified value and data type.
    /// </summary>
    private static Cell CreateCell(string value, CellValues dataType)
    {
        return new Cell
        {
            CellValue = new CellValue(value),
            DataType = dataType
        };
    }

    /// <summary>
    /// Gets the value of a cell, handling shared strings.
    /// </summary>
    private static string GetCellValue(WorkbookPart workbookPart, Cell cell)
    {
        if (cell.CellValue == null)
        {
            return string.Empty;
        }

        var value = cell.CellValue.Text;

        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            var stringTable = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (stringTable != null)
            {
                return stringTable.ElementAt(int.Parse(value)).InnerText;
            }
        }

        return value ?? string.Empty;
    }

    /// <summary>
    /// Parses adapter type from string.
    /// </summary>
    private static AdapterType ParseAdapterType(string value, int rowNumber, List<string> errors)
    {
        if (Enum.TryParse<AdapterType>(value, true, out var result))
        {
            return result;
        }

        errors.Add($"Row {rowNumber}: Invalid adapter type '{value}'. Using default 'Sse'.");
        return AdapterType.Sse;
    }

    /// <summary>
    /// Parses boolean value from string.
    /// </summary>
    private static bool ParseBool(string value, bool defaultValue)
    {
        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        if (value.Equals("1") || value.Equals("yes", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Equals("0") || value.Equals("no", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return defaultValue;
    }

    /// <summary>
    /// Parses integer value from string.
    /// </summary>
    private static int ParseInt(string value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    /// Parses headers from JSON string.
    /// </summary>
    private static Dictionary<string, string>? ParseHeaders(string value, int rowNumber, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(value);
        }
        catch
        {
            errors.Add($"Row {rowNumber}: Invalid JSON format for headers. Using empty headers.");
            return new Dictionary<string, string>();
        }
    }
}


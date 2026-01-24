
//==== Standard Excel Samples =============================


/*
 * This script unhides all rows and columns on a given worksheet.
 * This script does not affect any pre-existing data in the workbook.
 */
function main(workbook: ExcelScript.Workbook) {
  // Get the currently selected sheet.
  const selectedSheet = workbook.getActiveWorksheet();

  // Get the entire data range.
  const range = selectedSheet.getUsedRange();

  // If the used range is empty, end the script.
  if (!range) {
    console.log(`No data on this sheet.`);
    return;
  }

  // Log the address of the used range.
  console.log(`Used range for the worksheet: ${range.getAddress()}`);

  // If no columns are hidden, log message, else, unhide columns
  if (range.getColumnHidden() === false) {
    console.log(`No columns hidden`);
  } else {
    range.setColumnHidden(false);
  }

  // If no rows are hidden, log message, else, unhide rows.
  if (range.getRowHidden() === false) {
    console.log(`No rows hidden`);
  } else {
    range.setRowHidden(false);
  }
}


/*
 * This script will take a selected range of a sheet and freeze the selection.
 * This script does not affect pre-existing data in the workbook.
 */
function main(workbook: ExcelScript.Workbook) {
  // Get the currently selected sheet.
  const selectedSheet = workbook.getActiveWorksheet();

  // Get the current selected range.
  const selectedRange = workbook.getSelectedRange();

  // If no cells are selected, end the script.
  if (selectedRange === null) {
    console.log(`No cells in worksheet selected.`);
    return;
  }

  // Log the address of the selected range
  console.log(
    `Selected range for the worksheet: ${selectedRange.getAddress()}`
  );

  // Freeze the selected range.
  selectedSheet.getFreezePanes().freezeAt(selectedRange);
}



/**
 * This script takes the selected range of a table and creates a new table from it.
 * Make sure to highlight a portion of a table before running the script.
 * Try running it with a button! Go to "..." -> "Add Button"
 */
function main(workbook: ExcelScript.Workbook) {
  // Get the selected range.
  let selectedRange = workbook.getSelectedRange();

  // Get the data values of the selected range
  let data = selectedRange.getValues();

  // Get how many columns and rows have been selected.
  // We need this to compute the range of the new table.
  const amtColumnsSelected = selectedRange.getColumnCount();
  const amtRowsSelected = selectedRange.getRowCount();

  // Find the table that is encompassing the selected range
  let parentTable = selectedRange.getTables()[0];

  // Get the headers of the table
  // If there are no headers, that means an invalid selection was made.
  // Tell the user to make a valid selection and terminate the script.
  try {
    // Get the header range
    var headers = parentTable.getHeaderRowRange();
  } catch (e) {
    console.log("Please make a selection inside a table.");
    return;
  }

  // Get the current worksheet we're working in
  let worksheet = parentTable.getWorksheet();

  // We're going to do some math to calculate the header row we need.
  // We want to return only the headers within the same columns as the selected range.
  // The getRangeByIndexes has the following definition:
  // getRangeByIndexes(startRow, startColumn, rowCount, columnCount)
  // We are taking the intersection of the header range and the selected range (extended to the header row) to grab the valid headers.
  let headersToKeep = headers.getIntersection(
    worksheet.getRangeByIndexes(
      headers.getRowIndex(),
      selectedRange.getColumnIndex(),
      headers.getRowCount(),
      selectedRange.getColumnCount()
    )
  );

  // Make a new sheet to store the new table
  let subTableSheet = workbook.addWorksheet();

  // Calculate the range for the new table.
  // -> The first row is the header row, so we need to add 1 to the row count.
  let tableRange = subTableSheet.getRangeByIndexes(
    0,
    0,
    amtRowsSelected + 1,
    amtColumnsSelected
  );

  // Add the headers to the first row of the new table
  data.unshift(headersToKeep.getValues()[0]);

  // Set the data
  tableRange.setValues(data);

  // Make excel recognize the range as a table and sort the values.
  let newTable = subTableSheet.addTable(tableRange, true);
  tableRange.select();
}




/*
 * This script removes all hyperlinks in the specified sheet.
 */
function main(workbook: ExcelScript.Workbook) {
  // Get the active worksheet.
  let sheet = workbook.getActiveWorksheet();

  // Get the used range to operate on.
  // For large ranges (over 10000 entries), consider splitting the operation into batches for performance.
  const targetRange = sheet.getUsedRange(true);

  // if the worksheet has no edited cells, finish running the script
  if (targetRange === undefined) {
    console.log(
      `The worksheet is empty, run on a sheet that has edited cells`
    );
    return;
  }
  console.log(
    `Target Range to clear hyperlinks from: ${targetRange.getAddress()}`
  );

  const rowCount = targetRange.getRowCount();
  const colCount = targetRange.getColumnCount();
  console.log(
    `Searching for hyperlinks in ${targetRange.getAddress()} which contains ${
      rowCount * colCount
    } cells`
  );

  // Go through each individual cell looking for a hyperlink.
  // This allows us to limit the formatting changes to only the cells with hyperlink formatting.
  let clearedCount = 0;
  for (let i = 0; i < rowCount; i++) {
    for (let j = 0; j < colCount; j++) {
      const cell = targetRange.getCell(i, j);
      const hyperlink = cell.getHyperlink();
      if (hyperlink) {
        cell.clear(ExcelScript.ClearApplyTo.hyperlinks);
        cell
          .getFormat()
          .getFont()
          .setUnderline(ExcelScript.RangeUnderlineStyle.none);
        cell.getFormat().getFont().setColor("Black");
        clearedCount++;
      }
    }
  }

  console.log(`Done. Cleared hyperlinks from ${clearedCount} cells`);
}





/*
 * This script counts the empty rows on a given worksheet, and can return them to a Power Automate Flow.
 * This script does not affect any pre-existing data in the workbook.
 */
function main(
  workbook: ExcelScript.Workbook,
  sheetName: string = "Sheet1"
): number {
  // Get the worksheet named "Sheet1".
  const sheet = workbook.getWorksheet(sheetName);

  // Get the entire data range.
  const range = sheet.getUsedRange(true);

  // If the used range is empty, end the script.
  if (!range) {
    console.log(`No data on this sheet.`);
    return;
  }

  // Log the address of the used range.
  console.log(`Used range for the worksheet: ${range.getAddress()}`);

  // Look through the values in the range for blank rows.
  const values = range.getValues();
  let emptyRows = 0;
  for (let row of values) {
    let emptyRow = true;

    // Look at every cell in the row for one with a value.
    for (let cell of row) {
      if (cell.toString().length > 0) {
        emptyRow = false;
      }
    }

    // If no cell had a value, the row is empty.
    if (emptyRow) {
      emptyRows++;
    }
  }

  // Log the number of empty rows.
  console.log(`Total empty rows: ${emptyRows}`);

  // Return the number of empty rows for use in a Power Automate flow.
  return emptyRows;
}





// but how to add that to Excel ?!

function main(workbook: ExcelScript.Workbook) {
	// This action currently can't be recorded.
	// This action currently can't be recorded.
	let selectedCell = workbook.getActiveCell();
	let selectedSheet = workbook.getActiveWorksheet();
	// Select cell on selectedSheet offset by -5301 row(s) and -1 column(s) relative to selectedCell
	selectedCell.getOffsetRange(-5301, -1).select();
}
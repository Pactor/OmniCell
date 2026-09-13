import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "E:/Funcom/OmniCell/research-output";
const csvPath = `${outputDir}/unresolved-spawn-keys.csv`;
const xlsxPath = `${outputDir}/unresolved-spawn-keys.xlsx`;
const previewPath = `${outputDir}/unresolved-spawn-keys-preview.png`;

const csvText = await fs.readFile(csvPath, "utf8");
const workbook = await Workbook.fromCSV(csvText, { sheetName: "Unresolved keys" });
const sheet = workbook.worksheets.getItem("Unresolved keys");
const lastRow = csvText.trimEnd().split(/\r?\n/).length;

// CSV import intentionally preserves identifiers as text. Convert only counts.
const countValues = sheet.getRange(`C2:D${lastRow}`).values.map(row => row.map(Number));
sheet.getRange(`C2:D${lastRow}`).values = countValues;

sheet.showGridLines = false;
sheet.tabColor = "#1F4E78";
sheet.freezePanes.freezeRows(1);
sheet.freezePanes.freezeColumns(1);

const dataRange = sheet.getRange(`A1:F${lastRow}`);
dataRange.format.font = { name: "Arial", size: 10, color: "#1F1F1F" };
dataRange.format.verticalAlignment = "center";
sheet.getRange("A1:F1").format = {
  fill: "#1F4E78",
  font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
  rowHeight: 24,
};
sheet.getRange(`A2:A${lastRow}`).format.horizontalAlignment = "center";
sheet.getRange(`C2:D${lastRow}`).format.horizontalAlignment = "right";
sheet.getRange(`C2:D${lastRow}`).format.numberFormat = "#,##0";
sheet.getRange(`E2:F${lastRow}`).format.wrapText = true;
sheet.getRange(`A2:F${lastRow}`).format.rowHeight = 34;
sheet.getRange(`A1:A${lastRow}`).format.columnWidth = 11;
sheet.getRange(`B1:B${lastRow}`).format.columnWidth = 20;
sheet.getRange(`C1:D${lastRow}`).format.columnWidth = 14;
sheet.getRange(`E1:E${lastRow}`).format.columnWidth = 64;
sheet.getRange(`F1:F${lastRow}`).format.columnWidth = 51;

const table = sheet.tables.add(`A1:F${lastRow}`, true, "UnresolvedSpawnKeys");
table.style = "TableStyleMedium2";
table.showFilterButton = true;

sheet.getRange("H1:I1").values = [["Summary", "Count"]];
sheet.getRange("H2:H5").values = [["All unresolved keys"], ["Existing item records"], ["Nano-only records"], ["Current-client-only item"]];
sheet.getRange("I2:I5").formulas = [
  [`=COUNTA(A2:A${lastRow})`],
  [`=COUNTIFS(B2:B${lastRow},"item")`],
  [`=COUNTIFS(B2:B${lastRow},"nano-only")`],
  [`=COUNTIFS(B2:B${lastRow},"current-client item")`],
];
sheet.getRange("H1:I1").format = {
  fill: "#1F4E78",
  font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
};
sheet.getRange("H2:H5").format.font = { name: "Arial", size: 10 };
sheet.getRange("I2:I5").format = { font: { name: "Arial", size: 10, bold: true }, numberFormat: "#,##0", horizontalAlignment: "right" };

const sniffRows = [
  ["RLQU", "248256 Agent", "56238 Nano Crystal (Detain Suspect)", "221819 Hacked Corroded Crystal (Detain Suspect)", "Observe the item granted by opening the container"],
  ["FFXV", "248257 Bureaucrat", "46456 Nano Crystal (Basic Worker-Droid)", "220866 Dirty Money Shadow Crystal (Basic Worker-Droid)", "Observe the item granted by opening the container"],
  ["MIFS", "248262 Martial Artist", "28943 Nano Crystal (Iron Fist)", "221275 Badly Corroded Crystal (Iron Fist)", "Observe the item granted by opening the container"],
  ["NTCB", "300892 Keeper", "210529 Nano Crystal (Adaptive Ambient Restoration)", "222559 Cracked and Miskept Shadow Crystal (Ambient Restoration)", "Observe the item granted by opening the container"],
  ["STVK", "248265 Soldier", "70401 Nano Crystal (Partial Deflection Shield)", "221888 Corroded Crystal with Bullet Holes (Partial Deflection Shield)", "Observe the item granted by opening the container"],
];
sheet.getRange("H8:L8").values = [["Key", "Package", "Likely item", "Damaged alternative", "Missing evidence"]];
sheet.getRange("H9").write(sniffRows);
sheet.getRange("H8:L8").format = {
  fill: "#5B9BD5",
  font: { name: "Arial", size: 10, bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
};
sheet.getRange("H9:H13").format = { font: { name: "Arial", size: 10, bold: true }, horizontalAlignment: "center" };
sheet.getRange("I9:L13").format = { font: { name: "Arial", size: 10 }, wrapText: true };
sheet.getRange("H1:H32").format.columnWidth = 20;
sheet.getRange("I8:I13").format.columnWidth = 25;
sheet.getRange("J8:J13").format.columnWidth = 42;
sheet.getRange("K8:K13").format.columnWidth = 54;
sheet.getRange("L8:L13").format.columnWidth = 44;
sheet.getRange("H9:L13").format.rowHeight = 42;

sheet.getRange("H16:I16").values = [["Definition", "Only the five Arete keys above still require a profession-container sniff. The 15 Supercharged mappings are removed from the unresolved list."]];
sheet.getRange("H17:I17").values = [["Source", "Current client record audit and existing SpawnItem evidence tables, 2026-09-13."]];
sheet.getRange("H16:H17").format = { fill: "#D9EAF7", font: { name: "Arial", size: 10, bold: true } };
sheet.getRange("I16:I17").format = { font: { name: "Arial", size: 10, italic: true }, wrapText: true };
sheet.getRange("H16:I17").format.rowHeight = 36;

workbook.recalculate();

const check = await workbook.inspect({
  kind: "table",
  range: "'Unresolved keys'!A1:L17",
  include: "values,formulas",
  tableMaxRows: 12,
  tableMaxCols: 12,
  maxChars: 6000,
});
console.log(check.ndjson);

const tailCheck = await workbook.inspect({
  kind: "table",
  range: `'Unresolved keys'!A${lastRow - 2}:F${lastRow}`,
  include: "values,formulas",
  tableMaxRows: 3,
  tableMaxCols: 6,
  maxChars: 2500,
});
console.log(tailCheck.ndjson);

const errors = await workbook.inspect({
  kind: "match",
  searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A|#NUM!|#NULL!|#SPILL!|#CALC!",
  options: { useRegex: true, maxResults: 100 },
  summary: "final formula error scan",
});
console.log(errors.ndjson);

const preview = await workbook.render({
  sheetName: "Unresolved keys",
  range: "A1:L18",
  scale: 1,
  format: "png",
});
await fs.writeFile(previewPath, new Uint8Array(await preview.arrayBuffer()));

await fs.mkdir(outputDir, { recursive: true });
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(xlsxPath);
console.log(`Saved ${xlsxPath}`);

from typing import List
from openpyxl.worksheet.worksheet import Worksheet
from openpyxl.cell import Cell

class WorksheetCache:
    def __init__(self, sheet: Worksheet):
        self.sheet: Worksheet = sheet
        self.cache_cells: List[Cell][Cell] = []
        row_index = 0
        for rows in sheet:
            row_index = row_index + 1
            neRow = []
            col_index = 0
            for cell in rows:
                col_index = col_index + 1
                neRow.append(cell.value)
                if col_index >= sheet.max_column:
                    break
            if len(neRow) > 0:
                self.cache_cells.append(neRow)

            if row_index >= sheet.max_row:
                    break
                
    def cell(self, row: int, col: int):
        row = row - 1
        col = col - 1
        if row >= 0 and row < len(self.cache_cells):
            rows = self.cache_cells[row]
            if col >= 0 and col < len(rows):
                return rows[col]
        return None
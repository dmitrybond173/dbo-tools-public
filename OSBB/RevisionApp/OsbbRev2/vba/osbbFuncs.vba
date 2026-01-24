'
' Functions for OSBB revision
'
' Written by Dmitry Bond. @ August 2023
' (actually written in 2019 but now I'm restoring the code which I previously lost)
'


Function Ts2Time(ByVal ts1) As Date

    Dim s
    s = CStr(ts1)
    
    ' 123456789_123456789_123456
    ' 2009-11-19:16:29:09.156000

    Dim y, m, d, h, n, sec
    y = CInt(Left(s, 4))
    m = CInt(Mid(s, 6, 2))
    d = CInt(Mid(s, 9, 2))
    
    h = CInt(Mid(s, 12, 2))
    n = CInt(Mid(s, 15, 2))
    sec = CInt(Mid(s, 18, 2))
    
    Ts2Time = DateSerial(y, m, d) + TimeSerial(h, n, sec)
    
End Function

Function TimeDiff(ByVal ts1, ByVal ts2) As Long

    Dim t1, t2, d
    t1 = Ts2Time(ts1)
    t2 = Ts2Time(ts2)
    d = t2 - t1
    
    TimeDiff = d
    
End Function


'
' Functions for OSBB revision
'
' Written by Dmitry Bond. @ August 2023
' (actually written in 2019 but now I'm restoring the code which I previously lost)
'


Sub FixTs()
    
    Dim ws As Worksheet
    Set ws = ThisWorkbook.ActiveSheet
    
    Dim s
    Dim s1, s2, s3 As String
    Dim iRow, iCol As Integer
    iRow = 15
    iCol = 2
    
    Do
        s = ws.Cells(iRow, iCol)
        st = ws.Cells(iRow, iCol + 1)
        IsEmptyVal = (s = vbEmpty) Or (Trim(s) = "")
        
        If (Not IsEmptyVal) And (VarType(s) = vbString) Then
            s1 = Left(s, 2)
            s2 = Mid(s, 4, 2)
            s3 = Right(s, 4)
            s = s3 + "/" + s2 + "/" + s1
        
            ws.Cells(iRow, iCol) = s
            ws.Cells(iRow, iCol + 1) = st
        End If
        
        iRow = iRow + 1
    Loop While (Not IsEmptyVal)
        
End Sub


Sub FixDateSelected()
    
    ' Fix date text in selected cells
    ' Idea: bank returns dates as "MM.DD.YYYY" text, this code change that to "YYYY/MM/DD"
    
    Set rng = Application.Selection

    Dim s
    Dim s1, s2, s3 As String
    
    For Each cel In rng.Cells
        s = cel.Value
        
        If (VarType(s) = vbString) Then
            s1 = Left(s, 2)
            s2 = Mid(s, 4, 2)
            s3 = Right(s, 4)
            s = s3 + "/" + s2 + "/" + s1
        
            cel.Value = s
        End If
    Next
    
End Sub


Sub FixTimeSelected()
    
    ' Fix date text in selected cells
    ' Idea: bank returns dates as "HH.mm.SS" text, this code change that to "HH:mm:SS"
    
    Set rng = Application.Selection

    Dim s
    Dim s1, s2, s3 As String
    
    For Each cel In rng.Cells
        s = cel.Value
        
        If (VarType(s) = vbString) Then
            s1 = Left(s, 2)
            s2 = Mid(s, 4, 2)
            s3 = Right(s, 2)
            s = s1 + ":" + s2 + ":" + s3
        
            cel.Value = s
        End If
    Next
    
End Sub



Sub AssignPrefixByRefs()

    ' User should enter a text prefix to be added to all transactions referenced from selection
    ' Idea: on a "Ñòàòèñòèêà"(Statistic) sheet user can select any rows with refs to transactions
    ' then he press Alt+F8, select "AssignPrefixByRefs" macro to run, enter a text, press [OK]
    ' and this VBA script will add specified prefix to all referenced trasactions on "ÑÂÎÄÍÀß" sheet
    ' So, each transaction on a "Ñòàòèñòèêà"(Statistic) sheet in column "G" has a #NNNN number
    ' Such number is a row number on "ÑÂÎÄÍÀß" sheet

    Dim prefix
    prefix = InputBox("Please specify prefix", "Prefix to Assign", "")
    If prefix = "" Then
        'MsgBox "exit"
        Exit Sub
    End If
    
    Dim isRemovePrefix
    isRemovePrefix = False
    prefix = Trim(prefix)
    If Left(prefix, 1) = "-" Then
        isRemovePrefix = True
        prefix = Mid(prefix, 2, Len(prefix) - 1)
    End If
    
    'MsgBox "= Prefix:" & prefix
  
    Dim rng As Range
    Set rng = Application.Selection
    
    If rng.Count > 0 Then
        btn = MsgBox(CStr(rng.Count) & " cells in selection. Is it ok to continue?", vbOKCancel, "Confirmation")
        If btn <> vbOK Then
            Exit Sub
        End If
    End If
    
    Dim ws As Worksheet
    Set ws = rng.Worksheet
    
    Dim trgWs As Worksheet
    Set trgWs = ThisWorkbook.Sheets("ÑÂÎÄÍÀß")
    
    Dim cel As Range

    Dim s, s1, s2
    Dim iCol, iRow, iC, iR As Integer
    Dim p1, p2 As Single
            
    iCol = 7
    For Each cel In rng.Cells
    
        iR = cel.Row
        s = ws.Cells(iR, 7)
        sOrig = s
        
        If ((VarType(s) = vbString)) And (Left(s, 1) = "#") Then
            s = Replace(s, "#", "")
            While (Len(s) > 0) And (Left(s, 1) = "0")
              s = Mid(s, 2, Len(s) - 1)
            Wend
            iRow = CInt(s)
            
            iC = cel.Column
            iR = cel.Row
            
            p1 = ws.Cells(iR, 11)
            p2 = trgWs.Cells(iRow, 5)
            If p1 <> p2 Then
                MsgBox "Amount is not match: " & CStr(p1) & " vs. " & CStr(p2)
                Exit Sub
            End If
            
            'MsgBox cel.Worksheet.Name & ": [R:" & CStr(iR) & ", C:" & CStr(iC) & "]/[iR:" & CStr(iRow) & "] - amnt:" & CStr(p2)
            
            s1 = ws.Cells(iR, 12)
            s2 = trgWs.Cells(iRow, 7)
            If isRemovePrefix Then
                If Left(s2, Len(prefix)) = prefix Then
                    s2 = Mid(s2, Len(prefix) + 1, Len(s2) - Len(prefix))
                    trgWs.Cells(iRow, 7) = s2
                End If
            Else
                If Left(s2, Len(prefix)) <> prefix Then
                    s2 = prefix & s2
                    trgWs.Cells(iRow, 7) = s2
                End If
            End If
            
        End If
        
    Next

End Sub


Function IsValidTxRef(ByVal pRef As String, ByRef pRowIndex As Integer) As Boolean
    
    pRowIndex = -1
    
    Dim result As Boolean
    result = (Len(pRef) = 5) And (Left(pRef, 1) = "#")
    
    If result Then
    
        Dim id As String
        id = pRef
        id = Replace(id, "#", "")
        While (Len(id) > 0) And (Left(id, 1) = "0")
          id = Mid(id, 2, Len(id) - 1)
        Wend
        pRowIndex = CInt(id)
        
    End If
    
    IsValidTxRef = result

End Function


Sub BuildRefs(ByRef refs As Scripting.Dictionary)
    
    Dim wsStat As Worksheet
    Set wsStat = ThisWorkbook.Sheets("Ñòàòèñòèêà")

    Dim toSkip As New Scripting.Dictionary
    toSkip.Add "row#", 1
    toSkip.Add "", 1

    Dim z, txAmnt
    Dim s, txId As String
    Dim iRow, iCol, emptyCnt As Integer
    Dim txRowIdx As Integer
    
    Dim txRef As ClsTxRef

    iCol = 7
    iRow = 8
    emptyCnt = 0
    
    list = ""
    
    Do While emptyCnt < 100
        
        s = wsStat.Cells(iRow, iCol).Value
        
        txRowIdx = -1
        isValidRef = (Not toSkip.Exists(s)) And IsValidTxRef(s, txRowIdx)
        
        If isValidRef Then
        
            txId = CStr(s)
            txAmnt = wsStat.Cells(iRow, 11).Value
            
            'DBG:
            If "#0805" = txId Or 805 = txRowIdx Then ' Or 5302 = iRow Then
                s = "DBG!"
            End If
        
            If Not refs.Exists(txId) Then
                Set txRef = New ClsTxRef
                refs.Add txId, txRef
                txRef.TxRowId = txId
                txRef.TxRowIndex = txRowIdx
                txRef.TxAmount = txAmnt
            Else
                Set txRef = refs.Item(txId)
            End If
            txRef.RowNumbers.Add iRow
        
        Else
        
            emptyCnt = emptyCnt + 1
            
        End If
        
        iRow = iRow + 1
        
        'If iRow > 100 Then
        '    Exit Do
        'End If
        
    Loop
    
    'z = refs.Exists("#0805")
    's = "DBG"

End Sub


Sub BackResolvingActRefs()

    ' Idea: 1st need to build refs - txRow# with amount of money and with collection of rows on "Ñòàòèñòèêà" sheet
    ' Then loop through the rows "Àêòû" sheet and check if against the refs we built, then insert into column "Àêòû" ref to act

    Dim toSkip As New Scripting.Dictionary
    toSkip.Add "#0", 1
    toSkip.Add "", 1

    Dim wsStat As Worksheet
    Set wsStat = ThisWorkbook.Sheets("Ñòàòèñòèêà")
    
    Dim wsActs As Worksheet
    Set wsActs = ThisWorkbook.Sheets("Àêòû")

    Dim refs As New Scripting.Dictionary
    BuildRefs refs
    'MsgBox "cnt = " & CStr(refs.Count)
    
    Dim s
    Dim txRef As ClsTxRef
    Dim txId, txFile, xtxAmnt As String
    Dim iRow, iCol, emptyCnt As Integer
    Dim idx As Integer
    Dim statRowIdx
    iCol = 1
    iRow = 2
    
    Do While emptyCnt < 100
        
        s = wsActs.Cells(iRow, iCol).Value
        
        isValidRef = (Not toSkip.Exists(s)) And IsValidTxRef(s, idx) And refs.Exists(s)
        
        If isValidRef Then
        
            txId = CStr(s)
            txFile = wsActs.Cells(iRow, 3).Value
            txAmnt = wsActs.Cells(iRow, 4).Value
       
            Set txRef = refs.Item(txId)
            
            isOk = (Int(Abs(txRef.TxAmount)) = Int(Abs(txAmnt)))
            If isOk Then
            
                For Each statRowIdx In txRef.RowNumbers
                    wsStat.Cells(statRowIdx, 12).Value = txFile
                Next statRowIdx
                
            Else
                msg = "ERR:Money is not match: " & CStr(txRef.TxAmount) & " vs. " & CStr(txAmnt)
                
                For Each statRowIdx In txRef.RowNumbers
                    wsStat.Cells(statRowIdx, 12).Value = msg
                Next statRowIdx
                
                'Err.Raise 1111, msg ', [Description], [HelpFile], [HelpContext]
                'Exit Do
            End If
        
        Else
        
            emptyCnt = emptyCnt + 1
            
        End If
        
        iRow = iRow + 1
        
        'If iRow > 100 Then
        '    Exit Do
        'End If
        
    Loop
    
    MsgBox "End row# " & CStr(iRow)

End Sub



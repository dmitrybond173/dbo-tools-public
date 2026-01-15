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




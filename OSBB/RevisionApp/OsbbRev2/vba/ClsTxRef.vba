' ClsTxRef Class Module

Public TxRowId As String
Public TxRowIndex As String
Public TxAmount As Currency

Public RowNumbers As New Collection

Private Sub Class_Initialize()
    ' Code here runs automatically when a new object is created
    RowIndex = 0
End Sub

'Public Property Get RowIndex() As String
'    Name = pName
'End Property

'Public Sub Greet()
'    MsgBox "Hello, my name is " & pName & " and I am " & pAge & " years old."
'End Sub


﻿Imports StadisV4RProV9Plugin.WebReference
Imports System
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
'----------------------------------------------------------------------------------------------
'   Class: TenderDialogue
'    Type: Tender 
' Purpose: Called when tender button is pressed on tender screen. 
' Item Note1: STADIS\GiftCardID\IorA\CustomerID\Amount
' Item Note1: STADIS\GiftCardID\IorA\CustomerID\Amount\TranKey
' MANUAL_REMARK: StadisOpCode#CardID#AuthID
' MANUAL_REMARK: StadisOpCode#TranKey
'----------------------------------------------------------------------------------------------
<GuidAttribute(Discover.CLASS_TenderDialogue)> _
Public Class TenderDialogue
    Inherits TCustomTenderDialoguePlugin

    '----------------------------------------------------------------------------------------------
    ' Called during discovery
    ' Called when Sales/Receipts selected from menu (1)
    '----------------------------------------------------------------------------------------------
    Public Overrides Sub Initialize()
        MyBase.Initialize()
        fBusinessObjectType = Plugins.BusinessObjectType.btInvoice
        fDescription = "Stadis tender dialog"
        fGUID = New Guid(Discover.CLASS_TenderDialogue)
        fTenderType = gTenderDialogTenderType
        fUseDefaultDialog = False
    End Sub  'Initialize

    '----------------------------------------------------------------------------------------------
    ' Called when Sales/Receipts selected from menu (2)
    '----------------------------------------------------------------------------------------------
    Public Overrides Function Prepare() As Boolean
        UseDefaultDialog = False
        Return MyBase.Prepare()
    End Function  'Prepare

    '----------------------------------------------------------------------------------------------
    ' Called when button is pressed (1)
    '----------------------------------------------------------------------------------------------
    Public Overrides Sub Clear()
        MyBase.Clear()
    End Sub  'Clear

    '----------------------------------------------------------------------------------------------
    ' Called when button is pressed (2)
    '----------------------------------------------------------------------------------------------
    Public Overrides Function HandleEvent() As Boolean
        Dim mFrmStadisCharge As New FrmStadisCharge
        With mFrmStadisCharge
            .Adapter = fAdapter
            .ParentDialog = Me
        End With
        Select Case mFrmStadisCharge.ShowDialog()
            Case Windows.Forms.DialogResult.OK
                Return True
            Case Windows.Forms.DialogResult.Cancel
                Return False
        End Select
        mFrmStadisCharge = Nothing
        Return MyBase.HandleEvent()
    End Function  'HandleEvent

    '----------------------------------------------------------------------------------------------
    ' Called when deleting
    '----------------------------------------------------------------------------------------------
    Public Overrides Function DeleteTender() As Boolean
        If fTenderType = gTenderDialogTenderType Then
            Dim remark() As String = fRemark.Split("#"c)
            If remark.Length > 1 Then
                'If remark(0) = "@PR" Then
                '    '' Find Stadis tender this belongs to and reverse it
                '    'Dim invoiceHandle As Integer = 0
                '    'Dim tenderHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Tenders")
                '    'CommonRoutines.BOOpen(fAdapter, tenderHandle)
                '    'Dim tenderCount As Integer = CommonRoutines.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_COUNT")
                '    'If tenderCount > 0 Then
                '    '    CommonRoutines.BOFirst(fAdapter, tenderHandle, "TD - DeleteTender")
                '    '    While Not fAdapter.EOF(tenderHandle)
                '    '        Dim tenderType As Integer = CommonRoutines.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE")
                '    '        If tenderType = gTenderDialogTenderType Then
                '    '            Dim remark2() As String = CommonRoutines.BOGetStrAttributeValueByName(fAdapter, tenderHandle, "MANUAL_REMARK").Split("#"c)
                '    '            If remark2.Length > 0 AndAlso remark(1) = remark2(1) AndAlso (remark2(0) = "@TK" OrElse remark2(0) = "@GC") Then
                '    '                DoSVAccountReverse(fAdapter, remark2(2))
                '    '            End If
                '    '            If remark2.Length > 0 AndAlso remark(1) = remark2(1) Then
                '    'delete from rpro 

                '    '            End If
                '    '        End If
                '    '        fAdapter.BONext(tenderHandle)
                '    '    End While
                '    'End If

                '    Return True
                'End If
                Dim successful As Boolean = Common.DoSVAccountReverse(fAdapter, remark(2))
                If successful Then
                    gLastTenderID = ""
                    gLastAuthID = ""
                    Return True
                Else
                    MsgBox("Unable to delete tender from Stadis.", MsgBoxStyle.Exclamation, "STADIS Tender")
                    Return True
                End If
            End If
        Else
            Return MyBase.DeleteTender()
        End If
    End Function  'DeleteTender

    '----------------------------------------------------------------------------------------------
    ' Called when RPro is shut down
    '----------------------------------------------------------------------------------------------
    Public Overrides Sub Cleanup()
        fBusinessObjectType = Nothing
        fDescription = Nothing
        fGUID = Nothing
        fTenderType = Nothing
        fUseDefaultDialog = Nothing
        MyBase.CleanUp()
    End Sub  'Cleanup

End Class  'TenderDialogue


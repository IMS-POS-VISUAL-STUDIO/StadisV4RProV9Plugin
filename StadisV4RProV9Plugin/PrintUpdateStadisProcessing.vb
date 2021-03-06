﻿Imports StadisV4RProV9Plugin.WebReference
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
'----------------------------------------------------------------------------------------------
'   Class: PrintUpdateStadisProcessing
'    Type: EntityUpdate / BeforeUpdate on Invoice
' Purpose: Before invoice is saved, processes issues/activates, and posts Stadis transactions
' Item Note1: STADIS\GiftCardID\IorA\CustomerID\Amount
' Item Note1: STADIS\GiftCardID\IorA\CustomerID\Amount\TranKey
' MANUAL_REMARK: StadisOpCode#CardID#AuthID
' MANUAL_REMARK: StadisOpCode#TranKey
'----------------------------------------------------------------------------------------------
<GuidAttribute(Discover.CLASS_PrintUpdateStadisProcessing)> _
Public Class PrintUpdateStadisProcessing
    Inherits TCustomEntityUpdatePlugin

    Private mRProTenderCount As Integer = 0
    Private mTransactionShouldBeWritten As Boolean = False
    Private mReturnCash As Boolean = False
    Private mReturnGiftCard As Boolean = False
    Private mHasInserts As Boolean = False
    Private mItemCount As Integer = 0
    Private mTenderCount As Integer = 0
    Private mGiftCardCount As Integer = 0

    '----------------------------------------------------------------------------------------------
    ' Called when plugin is loaded
    '----------------------------------------------------------------------------------------------
    Public Overrides Sub Initialize()
        MyBase.Initialize()
        fEnabled = True
        fGUID = New Guid(Discover.CLASS_PrintUpdateStadisProcessing)
        fBusinessObjectType = Plugins.BusinessObjectType.btInvoice
    End Sub  'Initialize

    '----------------------------------------------------------------------------------------------
    ' Called when Print/Update is pressed, but before save
    ' Do appropriate web service calls
    '----------------------------------------------------------------------------------------------
    Public Overrides Function BeforeUpdate() As Boolean
        Try

            ' Get pointers to receipt components
            Dim invoiceHandle As Integer = 0
            Dim itemHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Items")
            Dim tenderHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Tenders")

            ''ToDo Remove when debugged
            'Dim itemCount As Integer = BOGetIntAttributeValueByName(Adapter, invoiceHandle, "Item Count")
            'Dim tenderCount As Integer = BOGetIntAttributeValueByName(Adapter, invoiceHandle, "Tender Count")
            'MsgBox("Items: " & itemCount.ToString & ", Tenders: " & tenderCount.ToString, MsgBoxStyle.Exclamation, "PUSP")

            ''Uncomment to get attribute lists
            'Dim attrlist As Object
            'attrlist = fAdapter.BOGetAttributeNameList(invoiceHandle)
            'Dim x As String = ""
            'attrlist = fAdapter.BOGetAttributeNameList(itemHandle)
            'Dim y As String = ""
            'attrlist = fAdapter.BOGetAttributeNameList(tenderHandle)
            'Dim z As String = ""

            If Adapter.BOIsAttributeInList(itemHandle, "Lookup ALU") = False Then
                Dim result As Integer = Adapter.BOIncludeAttrIntoList(itemHandle, "Lookup ALU", True, False)
                If result = Plugins.PluginError.peSuccess Then
                    Adapter.BOUpdateListDataSet(itemHandle, True, False)
                Else
                    MessageBox.Show("Unable to add 'Lookup ALU' to list")
                End If
            End If
            If Adapter.BOIsAttributeInInstance(itemHandle, "Lookup ALU") = False Then
                Dim result As Integer = Adapter.BOIncludeAttrIntoInstance(itemHandle, "Lookup ALU", True, False)
                If result = Plugins.PluginError.peSuccess Then
                    Adapter.BOUpdateInstanceDataSet(itemHandle, True, False)
                Else
                    MessageBox.Show("Unable to add 'Lookup ALU' to instance")
                End If
            End If

            mTransactionShouldBeWritten = False
            mReturnCash = False
            mGiftCardCount = 0
            mHasInserts = False

            ' Loop thru items and tenders and count them
            ' Check if there are Issues, Activates or Reloads
            ' If there are gift cards, make sure that there is a negative, offsetting tender or tenders.
            Dim processedOK As Boolean = PrepItemsAndTenders(invoiceHandle, itemHandle, tenderHandle)
            If processedOK = False Then Return False

            ' Receipt or return?
            Dim retval As Boolean = False
            If gIsAReturn = False Then
                retval = ProcessReceipt(invoiceHandle, itemHandle, tenderHandle)
            Else
                retval = ProcessReturn(invoiceHandle, itemHandle, tenderHandle)
            End If
            Return retval

        Catch ex As Exception
            MessageBox.Show("005 Error while processing STADIS transaction." & vbCrLf & ex.Message, "STADIS")
            Return False
        End Try
        Return MyBase.BeforeUpdate()
    End Function  'BeforeUpdate

    '----------------------------------------------------------------------------------------------
    ' Make sure that negative tender offset(s) equal gift card total
    '----------------------------------------------------------------------------------------------
    Private Function PrepItemsAndTenders(ByVal invoiceHandle As Integer, ByVal itemHandle As Integer, ByVal tenderHandle As Integer) As Boolean
        mItemCount = 0
        mTenderCount = 0
        Dim giftCardTotal As Decimal = 0D
        Dim offsetTotal As Decimal = 0D
        Try

            ' Check items
            Common.BOOpen(fAdapter, itemHandle)
            Common.BOFirst(fAdapter, itemHandle, "PUSP - PrepItemsAndTenders")
            While Not fAdapter.EOF(itemHandle)
                mItemCount += 1
                Dim alu As String
                Try
                    alu = Common.BOGetStrAttributeValueByName(fAdapter, itemHandle, "Lookup ALU")
                Catch ex As Exception
                    alu = "NoALU"
                End Try
                If IsAGiftCardALU(alu) Then
                    Dim Stadis() As String = Common.BOGetStrAttributeValueByName(fAdapter, itemHandle, "Item Note1").Split("\"c)
                    If Stadis(0) = "STADIS" Then
                        Select Case Stadis(2)
                            Case "I"
                                mHasInserts = True
                                mGiftCardCount += 1
                                giftCardTotal += CDec(Stadis(4))
                            Case "A"
                                mGiftCardCount += 1
                                giftCardTotal += CDec(Stadis(4))
                            Case Else
                                ' Keep reload offsets from hitting error routine
                        End Select
                    Else
                        Common.BOEdit(fAdapter, itemHandle)
                        Common.BODelete(fAdapter, itemHandle)
                        mItemCount -= 1
                        Common.BORefreshRecord(fAdapter, 0)
                        MessageBox.Show("Gift cards must be added by using the gift card button.", "STADIS")
                        Return False
                    End If
                End If
                fAdapter.BONext(itemHandle)
            End While

            ' Check tenders
            Common.BOOpen(fAdapter, tenderHandle)
            mRProTenderCount = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_COUNT")
            If mRProTenderCount > 0 Then
                Common.BOFirst(fAdapter, tenderHandle, "PUSP - PrepItemsAndTenders 2")
                While Not fAdapter.EOF(tenderHandle)
                    mTenderCount += 1
                    Dim tenderType As Integer = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE")
                    If tenderType = gTenderDialogTenderType Then
                        Dim tender As New TenderInfo(fAdapter, tenderHandle)
                        If tender.IsAStadisTender Then
                            mTransactionShouldBeWritten = True
                            If tender.IsAnOffset Then
                                offsetTotal += Common.BOGetDecAttributeValueByName(fAdapter, tenderHandle, "AMT")
                            End If
                            If tender.ShouldBeCharged AndAlso tender.IsAReturn Then
                                If gIssueGiftCardForReturn = True Then
                                    mReturnGiftCard = True
                                Else
                                    mReturnCash = True
                                End If
                            End If
                        End If
                    End If
                    fAdapter.BONext(tenderHandle)
                End While
            End If

            ' Check to make sure offset equals gift card total
            If gFeeOrTenderForIssueOffset = "Tender" Then
                Select Case giftCardTotal + offsetTotal
                    Case Is > 0
                        ' They inserted a gift card without going through our dialog - should have been caught above
                        MessageBox.Show("Gift card total does not agree with offsetting tender.", "STADIS")
                        Return False
                    Case Is < 0
                        ' They manually deleted a gift card
                        ReduceOffsetAmount(tenderHandle, -(giftCardTotal + offsetTotal))
                    Case Else
                        ' Everything OK - amounts offset
                End Select
            End If

            Return True

        Catch ex As Exception
            MessageBox.Show("020 Error while processing STADIS gift card(s)." & vbCrLf & ex.Message, "STADIS")
            Return False
        End Try
    End Function  'PrepItemsAndTenders

    Private Function IsAGiftCardALU(ByVal alu As String) As Boolean
        For Each gRow As DSGiftCardInfo.GiftCardInfoRow In gGCI.GiftCardInfo.Rows
            If gRow.RProLookupALU = alu Then
                Return True
            End If
        Next
        Return False
    End Function  'IsAGiftCardALU

    Private Function ReduceOffsetAmount(ByVal tenderHandle As Integer, ByVal difference As Decimal) As Boolean
        If mRProTenderCount > 0 Then
            Common.BOOpen(fAdapter, tenderHandle)
            Common.BOFirst(fAdapter, tenderHandle, "PUSP - ReduceOffsetAmount")
            While Not fAdapter.EOF(tenderHandle)
                Dim tenderType As Integer = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE")
                If tenderType = gTenderDialogTenderType Then
                    Dim tender As New TenderInfo(fAdapter, tenderHandle)
                    If tender.IsAnOffset Then
                        Dim offsetAmount As Decimal = Common.BOGetDecAttributeValueByName(fAdapter, tenderHandle, "AMT")
                        Common.BOEdit(fAdapter, tenderHandle)
                        Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "AMT", offsetAmount + difference)
                        Common.BOPost(fAdapter, tenderHandle)
                        Exit Function
                    End If
                End If
                fAdapter.BONext(tenderHandle)
            End While
        End If
    End Function  'ReduceOffsetAmount

#Region " Receipt Processing "

    '##############################################################################################
    '                                     RECEIPT PROCESSING
    '##############################################################################################
    ' Identify if we have any Issues, Activates or Reloads to be processed
    '----------------------------------------------------------------------------------------------
    Private Function ProcessReceipt(ByVal invoiceHandle As Integer, ByVal itemHandle As Integer, ByVal tenderHandle As Integer) As Boolean
        ' Post transaction
        Dim tranKey As Guid = GUID.Empty
        If mGiftCardCount > 0 Then
            Try
                tranKey = PostGCPurchase(invoiceHandle, itemHandle, tenderHandle)
                If tranKey = GUID.Empty Then Return False
                UpdateRProItemsAndTendersWithTranKey(tranKey, itemHandle, tenderHandle)
            Catch ex As Exception
                MessageBox.Show("015 Error while processing STADIS gift card purchase." & vbCrLf & ex.Message, "STADIS")
                Return False
            End Try
        ElseIf mTransactionShouldBeWritten = True OrElse gPostNonStadisTransactions = True Then
            Try
                tranKey = PostTransaction("Receipt", invoiceHandle, itemHandle, tenderHandle)
                If tranKey = GUID.Empty Then Return False
                UpdateRProItemsAndTendersWithTranKey(tranKey, itemHandle, tenderHandle)
            Catch ex As Exception
                MessageBox.Show("016 Error while processing STADIS transaction." & vbCrLf & ex.Message, "STADIS")
                Return False
            End Try
        End If
        Return True
    End Function  'ProcessReceipt

    '----------------------------------------------------------------------------------------------
    ' Construct and do a GCPurchase
    '----------------------------------------------------------------------------------------------
    Private Function PostGCPurchase(ByVal invoiceHandle As Integer, ByVal itemHandle As Integer, ByVal tenderHandle As Integer) As Guid
        Dim sr As New StadisRequest
        With sr
            .SiteID = gSiteID
            .TenderTypeID = 1
            .TenderID = ""
            .Amount = 0D
            .ReferenceNumber = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Number")
            '.CustomerID =  
            .VendorID = gVendorID
            .LocationID = gLocationID
            .RegisterID = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Workstion")
            .ReceiptID = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Number")
            .VendorCashier = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Cashier")
        End With
        Dim sy As StadisReply = Common.StadisAPI.GCPurchase(sr, LoadGiftCardsForWSCall(invoiceHandle, itemHandle), Common.LoadHeader(fAdapter, "Receipt", invoiceHandle), Common.LoadItems(fAdapter, "Receipt", invoiceHandle, itemHandle), Common.LoadTenders(fAdapter, "Receipt", invoiceHandle, tenderHandle))
        If sy.ReturnCode < 0 Then
            MsgBox(sy.ReturnMessage, MsgBoxStyle.Critical, "Error in PurchaseGiftCards.")
            Return GUID.Empty
        Else
            Return sy.TransactionKey
        End If
    End Function  'PostGCPurchase

    Private Function LoadGiftCardsForWSCall(ByVal invoiceHandle As Integer, ByVal itemHandle As Integer) As StadisGiftCard()
        Dim mStadisGiftCards(mGiftCardCount - 1) As StadisGiftCard
        Dim indx As Integer = 0
        Common.BOOpen(fAdapter, itemHandle)
        Common.BOFirst(fAdapter, itemHandle, "PUSP - LoadGiftCardsForWSCall")
        While Not fAdapter.EOF(itemHandle)
            ' Stadis() is used to parse the Stadis info we stored in Item Note1: STADIS\GiftCardID\IorA\CustomerID\Amount
            Dim Stadis() As String = Common.BOGetStrAttributeValueByName(fAdapter, itemHandle, "Item Note1").Split("\"c)
            If Stadis(0) = "STADIS" Then
                Dim itemType As String = Stadis(2)
                'If itemType = "I" Then mHasInserts = True
                If itemType = "I" OrElse itemType = "A" Then
                    Dim mStadisGiftCard As New StadisGiftCard
                    With mStadisGiftCard
                        .IssueOrActivate = Stadis(2)
                        .GiftCardID = Stadis(1)
                        .CustomerID = Stadis(3)
                        .Amount = CDec(Stadis(4))
                        '.Price = 0D
                        .TransactionKey = " "
                        .ALU = Common.BOGetStrAttributeValueByName(fAdapter, itemHandle, "Lookup ALU")
                    End With
                    mStadisGiftCards(indx) = mStadisGiftCard
                    indx += 1
                End If
            End If
            fAdapter.BONext(itemHandle)
        End While
        Return mStadisGiftCards
    End Function  'LoadGiftCardsForWSCall

    '----------------------------------------------------------------------------------------------
    ' Construct and post Stadis Transaction
    '----------------------------------------------------------------------------------------------
    Private Function PostTransaction(ByVal invoiceType As String, ByVal invoiceHandle As Integer, ByVal itemHandle As Integer, ByVal tenderHandle As Integer) As Guid
        Dim oRequest As New StadisRequest
        With oRequest
            .ReferenceNumber = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Number")
            .RegisterID = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Workstion")
            .ReceiptID = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Number")
            .VendorCashier = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Cashier")
            .TenderTypeID = 1
            .TenderID = ""
        End With
        Dim oReply As StadisReply = Common.StadisAPI.SVPostTransaction(oRequest, Common.LoadHeader(fAdapter, "Receipt", invoiceHandle), Common.LoadItems(fAdapter, "Receipt", invoiceHandle, itemHandle), Common.LoadTenders(fAdapter, "Receipt", invoiceHandle, tenderHandle))
        If oReply.ReturnCode < 0 Then
            MsgBox(oReply.ReturnMessage, MsgBoxStyle.Critical, "Error in PostTransaction.")
            Return GUID.Empty
        Else
            Return oReply.TransactionKey
        End If
    End Function  'PostTransaction

    '----------------------------------------------------------------------------------------------
    ' Write Stadis TransactionKey to RPro items and tenders
    '----------------------------------------------------------------------------------------------
    Private Sub UpdateRProItemsAndTendersWithTranKey(ByVal trankey As Guid, ByVal itemHandle As Integer, ByVal tenderHandle As Integer)

        'Update items
        Common.BOOpen(fAdapter, itemHandle)
        Common.BOFirst(fAdapter, itemHandle, "PUSP - UpdateRProItemsAndTendersWithTranKey I")
        While Not fAdapter.EOF(itemHandle)
            Dim stadisString As String = Common.BOGetStrAttributeValueByName(fAdapter, itemHandle, "Item Note1")
            Dim Stadis() As String = stadisString.Split("\"c)
            If Stadis(0) = "STADIS" Then
                Dim newStadisString As String = stadisString & "\" & trankey.ToString
                Common.BOEdit(fAdapter, itemHandle)
                Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Item Note1", newStadisString)
                Common.BOPost(fAdapter, itemHandle)
            End If
            fAdapter.BONext(itemHandle)
        End While

        'Update tenders
        If mRProTenderCount > 0 Then
            Common.BOOpen(fAdapter, tenderHandle)
            Common.BOFirst(fAdapter, tenderHandle, "PUSP - UpdateRProItemsAndTendersWithTranKey T")
            While Not fAdapter.EOF(tenderHandle)
                Dim tenderType As Integer = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE")
                If tenderType = gTenderDialogTenderType Then
                    Dim tender As New TenderInfo(fAdapter, tenderHandle)
                    If tender.IsAStadisTender Then
                        Common.BOEdit(fAdapter, tenderHandle)
                        Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "MANUAL_REMARK", tender.StadisOpCode & "#" & trankey.ToString)
                        Common.BOPost(fAdapter, tenderHandle)
                    End If
                End If
                fAdapter.BONext(tenderHandle)
            End While
        End If

    End Sub  'UpdateRProItemsAndTendersWithTranKey

#End Region  'Receipt Processing

#Region " Return, Cancel, and Reversal "

    '##############################################################################################
    '                                       RETURN PROCESSING
    '##############################################################################################
    ' If a return is being processed - positive in RPro, negative in Stadis
    '----------------------------------------------------------------------------------------------
    Private Function ProcessReturn(ByVal invoiceHandle As Integer, ByVal itemHandle As Integer, ByVal tenderHandle As Integer) As Boolean
        Try
            ' Post transaction
            Dim tranKey As Guid = GUID.Empty
            If mReturnGiftCard = True Then
                'TODO create GC
                Dim mFrmScanTicket As New FrmScanTicket
                mFrmScanTicket.Caption = "Refund as Gift Card? Enter card ID or cancel."
                Select Case mFrmScanTicket.ShowDialog()
                    Case Windows.Forms.DialogResult.OK
                        Dim tenderID As String = mFrmScanTicket.TicketID

                        tranKey = PostGCPurchase(invoiceHandle, itemHandle, tenderHandle)
                        If tranKey = GUID.Empty Then Return False
                        UpdateRProItemsAndTendersWithTranKey(tranKey, itemHandle, tenderHandle)
                        mFrmScanTicket = Nothing
                    Case Windows.Forms.DialogResult.Cancel
                        mReturnCash = True
                        mFrmScanTicket = Nothing
                End Select
            ElseIf mReturnCash = True Then
                ' Post transaction
                tranKey = PostTransaction("Return", invoiceHandle, itemHandle, tenderHandle)
                If tranKey = GUID.Empty Then Return False
                UpdateRProItemsAndTendersWithTranKey(tranKey, itemHandle, tenderHandle)
            ElseIf gPostNonStadisTransactions = True Then
                tranKey = PostTransaction("Return", invoiceHandle, itemHandle, tenderHandle)
                If tranKey = GUID.Empty Then Return False
                UpdateRProItemsAndTendersWithTranKey(tranKey, itemHandle, tenderHandle)
            End If
            Return True
        Catch ex As Exception
            MessageBox.Show("010 Error while processing STADIS transaction." & vbCrLf & ex.Message, "STADIS")
            Return False
        End Try
    End Function  'ProcessReturn

    Public Overrides Sub OnCancel()

        ' Get pointers to receipt components
        Dim invoiceHandle As Integer = 0
        Dim tenderHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Tenders")

        ' Reverse Stadis tenders
        Common.BOOpen(fAdapter, tenderHandle)
        mRProTenderCount = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_COUNT")
        If mRProTenderCount > 0 Then
            Common.BOFirst(fAdapter, tenderHandle, "PUSP - OnCancel")
            While Not fAdapter.EOF(tenderHandle)
                Dim tenderType As Integer = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE")
                If tenderType = gTenderDialogTenderType Then
                    Dim remark() As String = Common.BOGetStrAttributeValueByName(fAdapter, tenderHandle, "MANUAL_REMARK").Split("#"c)
                    If remark.Length > 0 AndAlso (remark(0) = "@TK" OrElse remark(0) = "@GC") Then
                        Common.DoSVAccountReverse(fAdapter, remark(2))
                    End If
                End If
                fAdapter.BONext(tenderHandle)
            End While
        End If

    End Sub  'OnCancel

    Public Overrides Function BeforeReversal(AEmplId As Integer, ByRef AComment As String, ByRef AComment2 As String) As Boolean

        ' Get pointers to receipt components
        Dim invoiceHandle As Integer = 0
        Dim tenderHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Tenders")

        ' Find a Stadis tender and get TransactionID from it
        Common.BOOpen(fAdapter, tenderHandle)
        mRProTenderCount = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_COUNT")
        If mRProTenderCount > 0 Then
            Common.BOFirst(fAdapter, tenderHandle, "PUSP - BeforeReversal")
            While Not fAdapter.EOF(tenderHandle)
                Dim tenderType As Integer = Common.BOGetIntAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE")
                If tenderType = gTenderDialogTenderType Then
                    Dim remark() As String = Common.BOGetStrAttributeValueByName(fAdapter, tenderHandle, "MANUAL_REMARK").Split("#"c)
                    If remark.Length > 0 AndAlso (remark(0) = "@TK" OrElse remark(0) = "@GC") Then
                        'Set up and do ReverseTransaction
                        Dim sr As New StadisRequest
                        With sr
                            .TransactionKey = New Guid(remark(1))
                            .SiteID = gSiteID
                            .TenderTypeID = 1
                            .TenderID = ""
                            .Amount = 0
                            .ReferenceNumber = ""
                            .VendorID = gVendorID
                            .LocationID = gLocationID
                            .RegisterID = Common.BOGetStrAttributeValueByName(fAdapter, 0, "Invoice Workstion")
                            .ReceiptID = Common.BOGetStrAttributeValueByName(fAdapter, 0, "Invoice Number")
                            .VendorCashier = Common.BOGetStrAttributeValueByName(fAdapter, 0, "Cashier")
                        End With
                        Dim sy As StadisReply = Common.StadisAPI.SVReverseTransaction(sr)
                        If sy.ReturnCode < 0 Then
                            MsgBox("Unable to reverse transaction in Stadis.", MsgBoxStyle.Exclamation, "Stadis")
                            Return MyBase.BeforeReversal(AEmplId, AComment, AComment2)
                        End If
                        Return MyBase.BeforeReversal(AEmplId, AComment, AComment2)
                    End If
                End If
                fAdapter.BONext(tenderHandle)
            End While
        End If

        Return MyBase.BeforeReversal(AEmplId, AComment, AComment2)
    End Function  'BeforeReversal

#End Region  'Cancel and Reverse

End Class  'PrintUpdateStadisProcessing

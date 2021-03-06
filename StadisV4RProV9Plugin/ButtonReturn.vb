﻿Imports StadisV4RProV9Plugin.WebReference
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
'----------------------------------------------------------------------------------------------
'   Class: ButtonReturn
'    Type: SideButton - Tender
' Purpose: Return a Stadis gift card
'----------------------------------------------------------------------------------------------
<GuidAttribute(Discover.CLASS_ButtonReturn)> _
Public Class ButtonReturn
    Inherits TCustomSideButtonPlugin

    '----------------------------------------------------------------------------------------------
    ' Called when plugin is loaded
    '----------------------------------------------------------------------------------------------
    Public Overrides Sub Initialize()
        MyBase.Initialize()
        fButtonEnabled = gReturnButtonEnabled
        fHint = gReturnButtonHint
        If gReturnButtonEnabled = True Then
            fCaption = gReturnButtonCaption
        Else
            fCaption = "Disabled"
        End If
        fPictureFilename = gReturnButtonImage
        fLayoutActionName = "actStadisReturnButton"
        fChecked = True
        fEnabled = gReturnButtonEnabled
        fGUID = New Guid(Discover.CLASS_ButtonReturn)
        fBusinessObjectType = Plugins.BusinessObjectType.btInvoice
    End Sub  'Initialize

    '----------------------------------------------------------------------------------------------
    ' Called when button is clicked
    '----------------------------------------------------------------------------------------------
    Public Overrides Function HandleEvent() As Boolean
        Try
            Dim invoiceHandle As Integer = 0
            Dim amtDueToCust As Object = fAdapter.BOGetAttributeValueByName(invoiceHandle, "Invc Total")
            If CDec(amtDueToCust) > 0 Then
                If gReturnButtonActive = True Then
                    Dim mFrmScanTicket As New FrmScanTicket
                    mFrmScanTicket.Caption = "Please Scan the Return Gift Card"
                    Select Case mFrmScanTicket.ShowDialog()
                        Case Windows.Forms.DialogResult.OK
                            If gAllowReturnCreditToCard = True AndAlso mFrmScanTicket.GiftCardExists AndAlso mFrmScanTicket.GiftCardIsActive = True Then
                                CreditReturnToExistingGiftCard(mFrmScanTicket.TicketID, CDec(amtDueToCust), mFrmScanTicket.GiftCardExists, mFrmScanTicket.EventID, mFrmScanTicket.Balance)
                            ElseIf gIssueGiftCardForReturn = True Then
                                PutReturnAmountOnNewGiftCard(mFrmScanTicket.TicketID, CDec(amtDueToCust), mFrmScanTicket.GiftCardExists, mFrmScanTicket.Balance)
                            Else
                                MessageBox.Show("System is not set to issue or credit gift cards for returns.", "STADIS")
                            End If
                            DoCharge(mFrmScanTicket.TicketID, CDec(amtDueToCust))
                        Case Windows.Forms.DialogResult.Cancel
                            'nothing
                    End Select
                    mFrmScanTicket = Nothing
                    invoiceHandle = Nothing
                End If
            Else
                MessageBox.Show("Enter return items before pressing this button.", "STADIS")
            End If
        Catch ex As Exception
            MessageBox.Show("Error while processing return." & vbCrLf & ex.Message, "STADIS")
        End Try
        Return MyBase.HandleEvent()
    End Function  'HandleEvent

    '----------------------------------------------------------------------------------------------
    ' Enables/disables button based on Receipt Type
    '----------------------------------------------------------------------------------------------
    Public Overrides ReadOnly Property ButtonEnabled() As Boolean
        Get
            If gIsAReturn = True Then
                fEnabled = True
            Else
                fEnabled = False
            End If
            Return MyBase.Enabled
        End Get
    End Property

    '------------------------------------------------------------------------------
    ' Create tender to card
    '------------------------------------------------------------------------------
    Private Sub CreditReturnToExistingGiftCard(ByVal giftCardID As String, ByVal amtDueToCust As Decimal, ByVal ticketExists As Boolean, ByVal eventID As String, ByVal remainingBalance As Decimal)
        Try
            Dim invoiceHandle As Integer = 0
            'Create a tender with an offsetting negative balance
            Dim tenderHandle As Integer = fAdapter.GetReferenceBOForAttribute(0, "Tenders")
            Common.BOOpen(fAdapter, tenderHandle)
            Common.BOInsert(fAdapter, tenderHandle)
            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE", gTenderDialogTenderType)
            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "GIVEN", amtDueToCust)
            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "AMT", amtDueToCust)
            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "TRANSACTION_ID", giftCardID)
            If Common.IsAGiftCard(eventID) Then
                Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "MANUAL_REMARK", "@GR #" & giftCardID)
            Else
                Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "MANUAL_REMARK", "@TR #" & giftCardID)
            End If
            If gTenderDialogTenderType = Plugins.TenderTypes.ttGiftCard Then
                Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_EXP_MONTH", 1)
                Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_EXP_YEAR", 1)
                Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_TYPE", 1)
                Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_PRESENT", 1)
            End If
            Common.BOPost(fAdapter, tenderHandle)
            Common.BORefreshRecord(fAdapter, invoiceHandle)
            invoiceHandle = Nothing
            tenderHandle = Nothing
        Catch ex As Exception
            MessageBox.Show("Error while crediting STADIS gift card." & vbCrLf & ex.Message, "STADIS")
        End Try
    End Sub  'CreditReturnToExistingGiftCard

    '----------------------------------------------------------------------------------------------
    ' Check whether issue or activate; create tender with card info
    '----------------------------------------------------------------------------------------------
    Friend Sub PutReturnAmountOnNewGiftCard(ByVal giftCardID As String, ByVal amtDueToCust As Decimal, ByVal ticketExists As Boolean, ByVal remainingBalance As Decimal)
        Try

            If ticketExists Then
                If remainingBalance > 0D Then
                    MessageBox.Show("That gift card already exists.", "STADIS")
                    Exit Sub
                End If
            End If

            ' Create item for Gift Card
            Dim invoiceHandle As Integer = 0
            Common.BORefreshRecord(fAdapter, invoiceHandle)
            Dim itemHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Items")
            Dim itemType As String = ""
            If ticketExists Then
                itemType = "A"
            Else
                itemType = "I"
            End If
            Common.BOOpen(fAdapter, itemHandle)
            Common.BOInsert(fAdapter, itemHandle)
            Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Lookup ALU", gReturnGiftCardALU)
            Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Item Note1", "STADIS\" & giftCardID & "\" & itemType & "\ \" & amtDueToCust.ToString("""$""#,##0.00"))
            Dim len As Integer = giftCardID.Length
            Dim lastfour As String = giftCardID.Substring(len - 4, 4)
            Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Item Note2", lastfour)
            Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Item Note3", amtDueToCust.ToString("""$""#,##0.00"))
            Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Quantity", 1)
            Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Price", 0D)
            Common.BOSetAttributeValueByName(fAdapter, itemHandle, "Tax Amount", 0D)
            Common.BOPost(fAdapter, itemHandle)

            ' Create offsetting tender or fee
            Select Case gFeeOrTenderForIssueOffset
                Case "Fee"
                    If amtDueToCust > 0D Then
                        Common.BOOpen(fAdapter, invoiceHandle)
                        Dim fee As Decimal = Common.BOGetDecAttributeValueByName(fAdapter, invoiceHandle, "Fee Amt")
                        fee -= amtDueToCust  'since this is a return, fee is negative
                        Common.BOSetAttributeValueByName(fAdapter, invoiceHandle, "Fee Amt", fee)
                        Common.BOSetAttributeValueByName(fAdapter, invoiceHandle, "Fee Name", "STADIS")
                    End If
                Case "Tender"
                    Dim tenderHandle As Integer = fAdapter.GetReferenceBOForAttribute(0, "Tenders")
                    If amtDueToCust > 0D Then
                        Common.BOOpen(fAdapter, tenderHandle)
                        Common.BOInsert(fAdapter, tenderHandle)
                        Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "TENDER_TYPE", gTenderDialogTenderType)
                        Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "AMT", amtDueToCust)
                        Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "GIVEN", amtDueToCust)
                        Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "TRANSACTION_ID", "")
                        Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "MANUAL_REMARK", "@OF # Offset for card issue/activate")
                        If gTenderDialogTenderType = Plugins.TenderTypes.ttGiftCard Then
                            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_EXP_MONTH", 1)
                            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_EXP_YEAR", 1)
                            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_TYPE", 1)
                            Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "CRD_PRESENT", 1)
                        End If
                        Common.BOPost(fAdapter, tenderHandle)
                    End If
                    Common.BORefreshRecord(fAdapter, invoiceHandle)
                    tenderHandle = Nothing
                Case Else
                    MessageBox.Show("Invalid offset option specified.", "STADIS")
            End Select
            invoiceHandle = Nothing
            itemHandle = Nothing

        Catch ex As Exception
            MessageBox.Show("Error while adding STADIS gift card(s)." & vbCrLf & ex.Message, "STADIS")
        End Try

    End Sub  'PutReturnAmountOnNewGiftCard

    Private Function DoCharge(ByVal tenderID As String, ByVal amount As Decimal) As Boolean
        Try

            ' Get pointers to receipt components
            Dim invoiceHandle As Integer = 0
            Dim itemHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Items")
            Dim tenderHandle As Integer = fAdapter.GetReferenceBOForAttribute(invoiceHandle, "Tenders")

            Dim sr As New StadisRequest
            With sr
                .SiteID = gSiteID
                .TenderTypeID = 3
                .TenderID = tenderID
                .Amount = -amount
                .ReferenceNumber = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Number")
                '.CustomerID =  
                .VendorID = gVendorID
                .LocationID = gLocationID
                .RegisterID = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Workstion")
                .ReceiptID = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Invoice Number")
                .VendorCashier = Common.BOGetStrAttributeValueByName(fAdapter, invoiceHandle, "Cashier")
            End With
            Dim sy As StadisReply() = Common.StadisAPI.SVAccountCharge(sr, Common.LoadHeader(fAdapter, "Receipt", invoiceHandle), Common.LoadItems(fAdapter, "Receipt", invoiceHandle, itemHandle), Common.LoadTenders(fAdapter, "Receipt", invoiceHandle, tenderHandle))
            Select Case sy(0).ReturnCode
                Case 0, -2
                    'All statuses good - or customer inactive - Update Rpro with AuthID
                    Dim remainingBalance As Decimal = sy(0).FromSVAccountBalance
                    Common.BOEdit(fAdapter, tenderHandle)
                    Dim paddedRemAmt As String = Space(8 - Len(sy(0).FromSVAccountBalance.ToString("#0.00"))) & sy(0).FromSVAccountBalance.ToString("#0.00")
                    Common.BOSetAttributeValueByName(fAdapter, tenderHandle, "AUTH", sy(0).StadisAuthorizationID & "\" & paddedRemAmt)
                    Common.BOPost(fAdapter, tenderHandle)
                    Return True
                Case -1
                    MsgBox("Ticket/Card is Inactive!" & vbCrLf & "Ticket/Card: " & tenderID, MsgBoxStyle.Exclamation, "Ticket Tender")
                    Return False
                Case -3
                    MsgBox("Ticket not found!" & vbCrLf & "Ticket/Card: " & tenderID, MsgBoxStyle.Exclamation, "Ticket Tender")
                    Return False
                Case -99
                    MsgBox("Charge failed for unknown reasons!" & vbCrLf & "Try again later.", MsgBoxStyle.Exclamation, "Ticket Tender")
                    Return False
            End Select
            invoiceHandle = Nothing
            itemHandle = Nothing
            tenderHandle = Nothing

        Catch ex As Exception
            MsgBox(ex.Message.ToString(), , "SVAccountCharge")
            Return False
        End Try
        Return False

    End Function  'DoCharge

End Class  'ButtonReturn

﻿Imports CommonV4.WebReference
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
'----------------------------------------------------------------------------------------------
'   Class: Common
'    Type: Common subroutines
' Purpose: Subroutines called from more than one class
'----------------------------------------------------------------------------------------------
Public Class CommonRoutines

#Region " Data Declarations "

    Private Shared mService As StadisV4TransactionsService

#End Region  'Data Declarations

#Region " Methods "

    '----------------------------------------------------------------------------------------------
    ' Provides access to web service; creates connection if it doesn't exist
    '----------------------------------------------------------------------------------------------
    Public Shared Function StadisAPI() As StadisV4TransactionsService
        If mService Is Nothing Then
            ' Create the web service proxy
            mService = New StadisV4TransactionsService
            ' Create the security credentials
            Dim cred As New SecurityCredentials
            cred.UserID = gStadisUserID
            cred.Password = gStadisPassword
            ' Provide the credentials to the service proxy
            mService.SecurityCredentialsValue = cred
            mService.Url = gStadisV4WebServiceURL
        End If
        'Provide the service proxy for use
        Return mService
    End Function  'StadisAPI

    Public Shared Sub Reset()
        mService = Nothing
    End Sub  'Reset

    '------------------------------------------------------------------------------
    ' Returns ReceiptID: RegisterID followed by seconds since start of current year
    '------------------------------------------------------------------------------
    Public Shared Function GetReceiptID(ByVal RegisterID As String) As String
        Dim dateNow As DateTime = Date.Now
        Dim yyyy As Integer = dateNow.Year
        Dim addYears As Integer = yyyy - 2001
        Dim centuryBegin As DateTime = #1/1/2001#
        Dim yearBegin As Date = centuryBegin.AddYears(addYears)
        Dim elapsedTicks As Long = dateNow.Ticks - yearBegin.Ticks
        Dim elapsedSpan As New TimeSpan(elapsedTicks)
        Dim elapsedDec As Decimal = CDec(elapsedSpan.TotalSeconds)
        Return RegisterID & CStr(Decimal.Round(elapsedDec, 0))
    End Function  'GetReceiptID

    '------------------------------------------------------------------------------
    ' Checks tender EventID against list from configuration settings
    '------------------------------------------------------------------------------
    Public Shared Function IsAGiftCard(ByVal eventID As String) As Boolean
        Dim mIsGiftCard As Boolean = False
        For i As Integer = 0 To gGiftCardEvent.Length - 1
            If eventID = gGiftCardEvent(i) Then
                mIsGiftCard = True
            End If
        Next
        Return mIsGiftCard
    End Function  'IsAGiftCard

    '------------------------------------------------------------------------------
    ' Strip out barcode from scanner input 
    '------------------------------------------------------------------------------
    Public Shared Function ExtractScan(ByVal scanstring As String) As String
        Dim extract As String = Regex.Match(scanstring, gExtractPattern).ToString
        If extract Is Nothing OrElse extract = "" Then
            Return scanstring
        Else
            Return extract
        End If
    End Function  'ExtractScan

    '------------------------------------------------------------------------------
    ' Validate barcode length, number ranges etc., using regex pattern from config
    '------------------------------------------------------------------------------
    Public Shared Function ValidateScan(ByVal scanInput As String) As Boolean
        Return Regex.IsMatch(scanInput, gValidatePattern)
    End Function  'ValidateScan

    '------------------------------------------------------------------------------
    ' Storage area for charge info
    '------------------------------------------------------------------------------
    Public Shared stadisChargeList As New List(Of StadisCharge)
    Public Class StadisCharge

        Private gTenderTypeID As Integer = 0
        Private gTenderIDFlag As String = ""
        Private gTenderID As String = ""
        Private gAmount As Decimal = 0D
        Private gStadisAuthorizationID As String = ""
        Private gMatchFound As Boolean = False

        Public Property TenderTypeID() As Integer
            Get
                Return gTenderTypeID
            End Get
            Set(ByVal Value As Integer)
                gTenderTypeID = Value
            End Set
        End Property

        Public Property TenderIDFlag() As String
            Get
                Return gTenderIDFlag
            End Get
            Set(ByVal Value As String)
                gTenderIDFlag = Value
            End Set
        End Property

        Public Property TenderID() As String
            Get
                Return gTenderID
            End Get
            Set(ByVal Value As String)
                gTenderID = Value
            End Set
        End Property

        Public Property Amount() As Decimal
            Get
                Return gAmount
            End Get
            Set(ByVal Value As Decimal)
                gAmount = Value
            End Set
        End Property

        Public Property StadisAuthorizationID() As String
            Get
                Return gStadisAuthorizationID
            End Get
            Set(ByVal Value As String)
                gStadisAuthorizationID = Value
            End Set
        End Property

        Public Property MatchFound() As Boolean
            Get
                Return gMatchFound
            End Get
            Set(ByVal Value As Boolean)
                gMatchFound = Value
            End Set
        End Property

        Public Sub New(ByVal tendertypeid As Integer, ByVal flag As String, ByVal tenderid As String, ByVal amt As Decimal, ByVal authid As String)
            gTenderTypeID = tendertypeid
            gTenderIDFlag = flag
            gTenderID = tenderid
            gAmount = amt
            gStadisAuthorizationID = authid
        End Sub

        Public Sub New()
        End Sub

    End Class  'StadisCharge

    '----------------------------------------------------------------------------------------------
    ' Get site and WS configuration settings
    '----------------------------------------------------------------------------------------------
    Public Shared Sub LoadInstallationSettings()

        If gAlreadyLoaded = True Then Exit Sub
        gAlreadyLoaded = True

        Try
            ' Get installation settings - standard first, then overrides, if any
            Dim mis As MessageAndInstallationSettings = CommonRoutines.StadisAPI.GetInstallationSettings("All")
            If mis.ReturnMessage.ReturnCode < 0 Then
                MessageBox.Show("Unable to access InstallationSettings.", "STADIS")
                Exit Sub
            End If
            SetSettingsFor(mis, "Site")
            SetSettingsFor(mis, gStandardSettingComponent)
            If gOverrideSettingComponent <> "" Then
                SetSettingsFor(mis, gOverrideSettingComponent)
            End If
        Catch ex As Exception
            gBalChkButtonActive = False
            gIssueButtonActive = False
            gRedeemButtonActive = False
            gReturnButtonActive = False
            gReloadButtonActive = False
            gBalChkButtonEnabled = False
            gIssueButtonEnabled = False
            gRedeemButtonEnabled = False
            gReturnButtonEnabled = False
            gReloadButtonEnabled = False
            MessageBox.Show("Unable to access InstallationSettings." & vbCrLf & ex.Message, "STADIS")
            Exit Sub
        End Try

        Try
            ' Get Gift Card table
            Dim msis As MessageAndSaleItems = CommonRoutines.StadisAPI.GetSaleItemsForRPro(gVendorID)
            If msis.ReturnMessage.ReturnCode < 0 Then
                MessageBox.Show("Unable to load gift card specifications.", "STADIS")
                gIssueButtonActive = False
            End If
            Dim gcis As SaleItem() = msis.SaleItems
            For i As Integer = 0 To gcis.Length - 1
                Dim gci As DSGiftCardInfo.GiftCardInfoRow = CType(gGCI.GiftCardInfo.NewRow, DSGiftCardInfo.GiftCardInfoRow)
                gci.GiftCardInfoID = gcis(i).SaleItemID
                gci.GiftCardName = gcis(i).Description
                gci.RProLookupALU = gcis(i).RProLookupALU
                gci.EventID = gcis(i).EventID
                gci.ButtonPosition = gcis(i).ButtonOrderRPro
                gci.ButtonCaption = gcis(i).ButtonCaption
                gci.DropdownCaption = gcis(i).DropdownCaption
                gci.ReceiptCaption = gcis(i).ReceiptCaption
                gci.FixedOrVariable = gcis(i).FixedOrVariable
                gci.AllowIssue = gcis(i).IsIssueAllowed
                gci.AllowActivate = gcis(i).IsActivateAllowed
                gci.IAMinAmount = gcis(i).IAMinAmount
                gci.IAMaxAmount = gcis(i).IAMaxAmount
                gci.AllowReload = gcis(i).IsReloadAllowed
                gci.RMinAmount = gcis(i).RMinAmount
                gci.RMaxAmount = gcis(i).RMaxAmount
                gGCI.GiftCardInfo.Rows.Add(gci)
            Next

            ' Derived settings
            gGiftCardEvent = Split(gGiftCardEvents, ";")
            Select Case gTenderTypeForStadis
                Case "GiftCard"
                    gStadisTenderType = RetailPro.Plugins.TenderTypes.ttGiftCard
                Case "ForeignCheck"
                    gStadisTenderType = RetailPro.Plugins.TenderTypes.ttForeignCheck
                Case "Check"
                    gStadisTenderType = RetailPro.Plugins.TenderTypes.ttCheck
            End Select

            Select Case gTenderDialogTender
                Case "Charge"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttCharge
                Case "Check"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttCheck
                Case "COD"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttCOD
                Case "Credit Card"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttCreditCard
                Case "Debit Card"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttDebitCard
                Case "Check in F/C"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttForeignCheck
                Case "Foreign Currency"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttForeignCurrency
                Case "Gift Card"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttGiftCard
                Case "Gift Certificate"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttGiftCertificate
                Case "Store Credit"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttStoreCredit
                Case "Traveler Check"
                    gTenderDialogTenderType = RetailPro.Plugins.TenderTypes.ttTravelerCheck
                Case Else
                    MessageBox.Show("Invalid Stadis TenderType specified - " & gTenderDialogTender, "STADIS")
            End Select

        Catch ex As Exception
            gBalChkButtonActive = False
            gIssueButtonActive = False
            gRedeemButtonActive = False
            gReturnButtonActive = False
            gReloadButtonActive = False
            gBalChkButtonEnabled = False
            gIssueButtonEnabled = False
            gRedeemButtonEnabled = False
            gReturnButtonEnabled = False
            gReloadButtonEnabled = False
            MessageBox.Show("Error loading Gift Card table." & vbCrLf & ex.Message, "STADIS")
        End Try

    End Sub  'LoadInstallationSettings

    Private Shared Sub SetSettingsFor(ByRef mis As MessageAndInstallationSettings, ByVal loadComponentName As String)
        For Each setting As InstallationSetting In mis.InstallationSettings
            With setting
                If .ComponentName = loadComponentName Then
                    Select Case .SettingName
                        'Site settings
                        Case "StadisVersion"
                            gStadisVersion = .SettingValue
                        Case "StadisRelease"
                            gStadisRelease = .SettingValue
                        Case "SiteSVType"
                            gSiteSVType = .SettingValue

                            'RPro settings
                        Case "TenderTypeForStadis"
                            gTenderTypeForStadis = .SettingValue
                        Case "FeeOrTenderForIssueOffset"
                            gFeeOrTenderForIssueOffset = .SettingValue
                        Case "FeeOrTenderForReloadOffset"
                            gFeeOrTenderForReloadOffset = .SettingValue
                        Case "VendorID"
                            gVendorID = .SettingValue
                        Case "FormLogoImage"
                            gFormLogoImage = .SettingValue
                        Case "ImageTransparentColor"
                            Try
                                Dim rgb() As String = .SettingValue.Split(","c)
                                If rgb.Count = 3 Then
                                    gImageTransparentColor = Drawing.Color.FromArgb(CInt(rgb(0)), CInt(rgb(1)), CInt(rgb(2)))
                                End If
                            Catch ex As Exception

                            End Try
                        Case "AskForTicketOnRedeem"
                            gAskForTicketOnRedeem = CBool(.SettingValue)
                        Case "AskForTicketOnIssue"
                            gAskForTicketOnIssue = CBool(.SettingValue)
                        Case "DefaultCustomerID"
                            gDefaultCustomerID = .SettingValue
                        Case "AllowReturnCreditToCard"
                            gAllowReturnCreditToCard = CBool(.SettingValue)
                        Case "PostNonStadisTransactions"
                            gPostNonStadisTransactions = CBool(.SettingValue)
                        Case "ScanPattern"
                            gValidatePattern = .SettingValue
                        Case "ExtractPattern"
                            gExtractPattern = .SettingValue
                        Case "ValidatePattern"
                            gValidatePattern = .SettingValue
                        Case "GiftCardEvents"
                            gGiftCardEvents = .SettingValue
                        Case "IssueGiftCardForReturn"
                            gIssueGiftCardForReturn = CBool(.SettingValue)
                        Case "IsPrintingEnabled"
                            gIsPrintingEnabled = CBool(.SettingValue)
                        Case "StadisTenderText"
                            gStadisTenderText = .SettingValue
                        Case "IsMergeFunctionEnabled"
                            gIsMergeFunctionEnabled = CBool(.SettingValue)
                        Case "ShowSVActionGrid"
                            gShowSVActionGrid = CBool(.SettingValue)

                        Case "BalChkButtonActive"
                            gBalChkButtonActive = CBool(.SettingValue)
                            If gBalChkButtonActive = True Then
                                gBalChkButtonEnabled = True
                            Else
                                gBalChkButtonEnabled = False
                            End If
                        Case "BalChkButtonCaption"
                            gBalChkButtonCaption = .SettingValue
                        Case "BalChkButtonImage"
                            gBalChkButtonImage = .SettingValue
                        Case "BalChkButtonHint"
                            gBalChkButtonHint = .SettingValue

                        Case "IssueButtonActive"
                            gIssueButtonActive = CBool(.SettingValue)
                            If gIssueButtonActive = True Then
                                gIssueButtonEnabled = True
                            Else
                                gIssueButtonEnabled = False
                            End If
                        Case "IssueButtonEnabled"
                            gIssueButtonEnabled = CBool(.SettingValue)
                        Case "IssueButtonCaption"
                            gIssueButtonCaption = .SettingValue
                        Case "IssueButtonImage"
                            gIssueButtonImage = .SettingValue
                        Case "IssueButtonHint"
                            gIssueButtonHint = .SettingValue

                        Case "RedeemButtonActive"
                            gRedeemButtonActive = CBool(.SettingValue)
                            If gRedeemButtonActive = True Then
                                gRedeemButtonEnabled = True
                            Else
                                gRedeemButtonEnabled = False
                            End If
                        Case "RedeemSideButtonEnabled"
                            gRedeemButtonEnabled = CBool(.SettingValue)
                        Case "RedeemButtonCaption"
                            gRedeemButtonCaption = .SettingValue
                        Case "RedeemButtonImage"
                            gRedeemButtonImage = .SettingValue
                        Case "RedeemButtonHint"
                            gRedeemButtonHint = .SettingValue

                        Case "ReturnButtonActive"
                            gReturnButtonActive = CBool(.SettingValue)
                            gReturnButtonEnabled = False
                        Case "ReturnButtonEnabled"
                            gReturnButtonEnabled = CBool(.SettingValue)
                        Case "ReturnButtonCaption"
                            gReturnButtonCaption = .SettingValue
                        Case "ReturnButtonImage"
                            gReturnButtonImage = .SettingValue
                        Case "ReturnButtonHint"
                            gReturnButtonHint = .SettingValue

                        Case "ReloadButtonActive"
                            gReloadButtonActive = CBool(.SettingValue)
                            If gReloadButtonActive = True Then
                                gReloadButtonEnabled = True
                            Else
                                gReloadButtonEnabled = False
                            End If
                        Case "ReloadButtonEnabled"
                            gReloadButtonEnabled = CBool(.SettingValue)
                        Case "ReloadButtonCaption"
                            gReloadButtonCaption = .SettingValue
                        Case "ReloadButtonImage"
                            gReloadButtonImage = .SettingValue
                        Case "ReloadButtonHint"
                            gReloadButtonHint = .SettingValue

                        Case "TenderDialogActive"
                            gTenderDialogActive = CBool(.SettingValue)
                        Case "TenderDialogFormText"
                            gTenderDialogFormText = .SettingValue
                        Case "TenderDialogHeader"
                            gTenderDialogHeader = .SettingValue
                        Case "TenderDialogTenderIDLabel"
                            gTenderDialogTenderIDLabel = .SettingValue
                        Case "TenderDialogTender"
                            gTenderDialogTender = .SettingValue

                    End Select
                End If
            End With
        Next
    End Sub  'SetSettingsFor

    '------------------------------------------------------------------------------
    ' Repackage Header data in Stadis format
    '------------------------------------------------------------------------------
    Public Shared Function LoadHeader(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByVal invoiceType As String, ByVal invoiceHandle As Integer) As StadisTranHeader
        ''Debug code
        'Dim itemCount As Integer = BOGetIntAttributeValueByName(adapter, invoiceHandle, "Item Count")
        'Dim tenderCount As Integer = BOGetIntAttributeValueByName(adapter, invoiceHandle, "Tender Count")
        'MsgBox("Items: " & itemCount.ToString & ", Tenders: " & tenderCount.ToString, MsgBoxStyle.Exclamation, "Load")

        Dim mTransHeader As New StadisTranHeader
        With mTransHeader
            .TransactionKey = Guid.NewGuid
            .LocationID = BOGetStrAttributeValueByName(adapter, invoiceHandle, "Store No")
            .RegisterID = BOGetStrAttributeValueByName(adapter, invoiceHandle, "Invoice Workstion")
            .ReceiptID = BOGetStrAttributeValueByName(adapter, invoiceHandle, "Invoice Number")
            .VendorID = GlobalValues.gVendorID
            .VendorCashier = BOGetStrAttributeValueByName(adapter, invoiceHandle, "Cashier")
            .VendorDiscountPct = BOGetStrAttributeValueByName(adapter, invoiceHandle, "Discount Percent")
            .VendorDiscount = BOGetDecAttributeValueByName(adapter, invoiceHandle, "Discount Amount")
            .VendorTax = BOGetDecAttributeValueByName(adapter, invoiceHandle, "Tax Total")
            .VendorTip = 0D
            Select Case invoiceType
                Case "Receipt"
                    .SubTotal = BOGetDecAttributeValueByName(adapter, invoiceHandle, "Subtotal")
                    .Total = BOGetDecAttributeValueByName(adapter, invoiceHandle, "Invc Total")
                Case "Return"
                    .SubTotal = -(BOGetDecAttributeValueByName(adapter, invoiceHandle, "Subtotal"))
                    .Total = -(BOGetDecAttributeValueByName(adapter, invoiceHandle, "Invc Total"))
            End Select
        End With
        Return mTransHeader
    End Function  'LoadHeader

    '------------------------------------------------------------------------------
    ' Repackage Item data in Stadis format
    '------------------------------------------------------------------------------
    Public Shared Function LoadItems(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByVal invoiceType As String, ByVal invoiceHandle As Integer, ByVal itemHandle As Integer) As StadisTranItem()
        Dim itemList As New List(Of StadisTranItem)
        BOOpen(adapter, itemHandle)
        BOFirst(adapter, itemHandle, "CR - LoadItems")
        While Not adapter.EOF(itemHandle)
            Dim item As New StadisTranItem
            With item
                Try
                    .ItemID = BOGetStrAttributeValueByName(adapter, itemHandle, "Lookup ALU")
                Catch ex As Exception
                    .ItemID = "NoALU"
                End Try
                ' Stadis() is used to parse the Stadis info we stored in Item Note1: STADIS\GiftCardID\IorA\CustomerID\Amount
                Dim Stadis() As String = BOGetStrAttributeValueByName(adapter, itemHandle, "Item Note1").Split("\"c)
                If Stadis(0) = "STADIS" Then
                    .Description = BOGetStrAttributeValueByName(adapter, itemHandle, "Item Note1")
                Else
                    .Description = BOGetStrAttributeValueByName(adapter, itemHandle, "Desc1")
                End If
                .Dept = ""
                .Class = ""
                .SubClass = ""
                Dim DCS() As String = BOGetStrAttributeValueByName(adapter, itemHandle, "DCS Code").Split(" "c)
                If DCS.Length > 0 Then
                    .Dept = DCS(0)
                End If
                If DCS.Length > 2 Then
                    .Class = DCS(2)
                End If
                If DCS.Length > 4 Then
                    .SubClass = DCS(4)
                End If
                Select Case invoiceType
                    Case "Receipt"
                        .Quantity = BOGetIntAttributeValueByName(adapter, itemHandle, "Qty")
                        .Price = BOGetDecAttributeValueByName(adapter, itemHandle, "Price")
                    Case "Return"
                        .Quantity = -(BOGetIntAttributeValueByName(adapter, itemHandle, "Qty"))
                        .Price = -(BOGetDecAttributeValueByName(adapter, itemHandle, "Price"))
                End Select
                .Cost = BOGetDecAttributeValueByName(adapter, itemHandle, "Cost")
                .Tax = 0D
                .AdditionalTax = 0D
                .Discount = 0D
            End With
            itemList.Add(item)
            adapter.BONext(itemHandle)
        End While
        Dim mTransItems(itemList.Count - 1) As StadisTranItem
        itemList.CopyTo(mTransItems)
        Return mTransItems
    End Function  'LoadItems

    '------------------------------------------------------------------------------
    ' Repackage Tender data in Stadis format
    '------------------------------------------------------------------------------
    Public Shared Function LoadTendersForCharge(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByVal invoiceType As String, ByVal tenderHandle As Integer, ByVal stt As StadisTranTender) As StadisTranTender()
        Dim tenderList As New List(Of StadisTranTender)
        BOOpen(adapter, tenderHandle)
        Dim tenderCount As Integer = CommonRoutines.BOGetIntAttributeValueByName(adapter, tenderHandle, "TENDER_COUNT")
        If tenderCount > 0 Then
            BOFirst(adapter, tenderHandle, "CR - LoadTendersForCharge")
            While Not adapter.EOF(tenderHandle)
                Dim tender As New StadisTranTender
                With tender
                    .IsStadisTender = False
                    .StadisAuthorizationID = " "
                    Dim rproTenderType As Integer = BOGetIntAttributeValueByName(adapter, tenderHandle, "TENDER_TYPE")
                    If rproTenderType <> gTenderDialogTenderType Then
                        .TenderTypeID = 0
                        .IsStadisTender = False
                        .StadisAuthorizationID = ""
                        .TenderID = ""
                    Else
                        Dim tInfo As New TenderInfo(adapter, tenderHandle)
                        If tInfo.IsAStadisTender Then
                            If tInfo.ShouldBeCharged Then
                                If tInfo.IsAPromo Then
                                    .TenderTypeID = 11  'Promo
                                Else
                                    .TenderTypeID = 1  'Ticket
                                End If
                                Dim remark() As String = CommonRoutines.BOGetStrAttributeValueByName(adapter, tenderHandle, "MANUAL_REMARK").Split("#"c)
                                If remark.Length > 0 Then
                                    .TenderID = remark(1)
                                    .StadisAuthorizationID = remark(2)
                                End If
                                .IsStadisTender = True
                            End If
                        Else
                            .TenderTypeID = 0
                        End If
                    End If
                    .Amount = BOGetDecAttributeValueByName(adapter, tenderHandle, "AMT")
                End With
                tenderList.Add(tender)
                adapter.BONext(tenderHandle)
            End While
        End If
        tenderList.Add(stt)
        Dim mTransTenders(tenderList.Count - 1) As StadisTranTender
        tenderList.CopyTo(mTransTenders)
        Return mTransTenders
    End Function  'LoadTendersForCharge

    '------------------------------------------------------------------------------
    ' Repackage Tender data in Stadis format
    '------------------------------------------------------------------------------
    Public Shared Function LoadTenders(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByVal invoiceType As String, ByVal invoiceHandle As Integer, ByVal tenderHandle As Integer) As StadisTranTender()
        Dim tenderList As New List(Of StadisTranTender)
        Dim tenderCount As Integer = BOGetIntAttributeValueByName(adapter, 0, "Tender Count")
        BOOpen(adapter, tenderHandle)
        If tenderCount > 0 Then
            'Build array of tenders to pass to web service PostTransaction
            BOFirst(adapter, tenderHandle, "CR - LoadTenders")
            While Not adapter.EOF(tenderHandle)
                Dim tender As New StadisTranTender
                With tender
                    .IsStadisTender = False
                    .StadisAuthorizationID = " "
                    Dim rproTenderType As Integer = BOGetIntAttributeValueByName(adapter, tenderHandle, "TENDER_TYPE")
                    If rproTenderType <> gTenderDialogTenderType Then
                        .TenderTypeID = 0
                        .IsStadisTender = False
                        .StadisAuthorizationID = ""
                        .TenderID = ""
                    Else
                        Dim ti As New TenderInfo(adapter, tenderHandle)
                        If ti.IsAStadisTender Then
                            If ti.ShouldBeCharged Then
                                If ti.IsAPromo Then
                                    .TenderTypeID = 11  'Promo
                                Else
                                    .TenderTypeID = 1  'Ticket
                                End If
                                Dim remark() As String = CommonRoutines.BOGetStrAttributeValueByName(adapter, tenderHandle, "MANUAL_REMARK").Split("#"c)
                                If remark.Length > 0 Then
                                    .TenderID = remark(1)
                                    .StadisAuthorizationID = remark(2)
                                End If
                                .IsStadisTender = True
                            ElseIf ti.IsAnOffset Then
                                .IsStadisTender = False
                                .StadisAuthorizationID = ""
                                If ti.IsAReturn Then
                                    .TenderTypeID = 4  'Gift Card
                                    Dim remark() As String = CommonRoutines.BOGetStrAttributeValueByName(adapter, tenderHandle, "MANUAL_REMARK").Split("#"c)
                                    If remark.Length > 0 Then
                                        .TenderID = remark(1)
                                    End If
                                Else
                                    .TenderTypeID = 0  'Other
                                    .TenderID = ""
                                End If
                            End If
                        Else
                            .TenderTypeID = 0
                        End If
                    End If
                    Select Case invoiceType
                        Case "Receipt"
                            .Amount = BOGetDecAttributeValueByName(adapter, tenderHandle, "AMT")
                        Case "Return"
                            .Amount = -(BOGetDecAttributeValueByName(adapter, tenderHandle, "AMT"))
                    End Select
                End With
                tenderList.Add(tender)
                adapter.BONext(tenderHandle)
            End While
            Dim mTransTenders(tenderList.Count - 1) As StadisTranTender
            tenderList.CopyTo(mTransTenders)
            Return mTransTenders
        Else
            'Build dummy tender to satisfy web service PostTransaction
            Dim mTransTenders(0) As StadisTranTender
            Dim mTransTender As New StadisTranTender
            With mTransTender
                .IsStadisTender = False
                .StadisAuthorizationID = " "
                .TenderTypeID = 0
                .TenderID = ""
                .Amount = 0D
            End With
            mTransTenders(0) = mTransTender
            Return mTransTenders
        End If
    End Function  'LoadTenders

    '------------------------------------------------------------------------------
    ' Repackage Tender data in Stadis format
    '------------------------------------------------------------------------------
    Public Shared Function OldLoadTendersForCharge(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByVal invoiceType As String, ByVal tenderHandle As Integer) As StadisTranTender()
        Dim tenderList As New List(Of StadisTranTender)
        BOOpen(adapter, tenderHandle)
        Dim tenderCount As Integer = CommonRoutines.BOGetIntAttributeValueByName(adapter, tenderHandle, "TENDER_COUNT")
        BOFirst(adapter, tenderHandle, "CR - LoadTendersForCharge")
        While Not adapter.EOF(tenderHandle)
            Dim tender As New StadisTranTender
            With tender
                .IsStadisTender = False
                .StadisAuthorizationID = " "
                Dim rproTenderType As Integer = BOGetIntAttributeValueByName(adapter, tenderHandle, "TENDER_TYPE")
                Dim stadisTenderType As Integer = ConvertRProTenderTypeToStadis(rproTenderType)
                If rproTenderType <> gStadisTenderType Then
                    .TenderTypeID = stadisTenderType
                    .IsStadisTender = False
                    .StadisAuthorizationID = ""
                    .TenderID = ""
                Else
                    Dim tInfo As New TenderInfo(adapter, tenderHandle)
                    If tInfo.IsAStadisTender Then
                        If tInfo.ShouldBeCharged Then
                            If tInfo.IsAPromo Then
                                .TenderTypeID = 11  'Promo
                            Else
                                .TenderTypeID = 1  'Ticket
                            End If
                            Dim remark() As String = CommonRoutines.BOGetStrAttributeValueByName(adapter, tenderHandle, "MANUAL_REMARK").Split("#"c)
                            If remark.Length > 0 Then
                                .TenderID = remark(1)
                                .StadisAuthorizationID = remark(2)
                            End If
                            .IsStadisTender = True
                        End If
                    Else
                        .TenderTypeID = stadisTenderType
                    End If
                End If
                .Amount = BOGetDecAttributeValueByName(adapter, tenderHandle, "AMT")
            End With
            tenderList.Add(tender)
            adapter.BONext(tenderHandle)
        End While
        Dim mTransTenders(tenderList.Count - 1) As StadisTranTender
        tenderList.CopyTo(mTransTenders)
        Return mTransTenders
    End Function  'OldLoadTendersForCharge

    '----------------------------------------------------------------------------------------------
    ' Return Stadis tender type corresponding to RPro tender type
    '----------------------------------------------------------------------------------------------
    Private Shared Function ConvertRProTenderTypeToStadis(ByVal rproTenderType As Integer) As Integer
        Dim ourTenderType As Integer = 0
        Select Case rproTenderType
            Case RetailPro.Plugins.TenderTypes.ttCash
                ourTenderType = 2
            Case RetailPro.Plugins.TenderTypes.ttCharge
                ourTenderType = 3
            Case RetailPro.Plugins.TenderTypes.ttCheck
                ourTenderType = 6
            Case RetailPro.Plugins.TenderTypes.ttCOD
                ourTenderType = 0
            Case RetailPro.Plugins.TenderTypes.ttCreditCard
                ourTenderType = 3
            Case RetailPro.Plugins.TenderTypes.ttDebitCard
                ourTenderType = 8
            Case RetailPro.Plugins.TenderTypes.ttDeposit
                ourTenderType = 0
            Case RetailPro.Plugins.TenderTypes.ttForeignCheck
                ourTenderType = 0
            Case RetailPro.Plugins.TenderTypes.ttForeignCurrency
                ourTenderType = 0
            Case RetailPro.Plugins.TenderTypes.ttGiftCard
                ourTenderType = 4
            Case RetailPro.Plugins.TenderTypes.ttGiftCertificate
                ourTenderType = 5
            Case RetailPro.Plugins.TenderTypes.ttPayments
                ourTenderType = 0
            Case RetailPro.Plugins.TenderTypes.ttStoreCredit
                ourTenderType = 0
            Case RetailPro.Plugins.TenderTypes.ttTravelerCheck
                ourTenderType = 0
            Case Else
                ourTenderType = 0
        End Select
        Return ourTenderType
    End Function  'ConvertRProTenderTypeToStadis

    '------------------------------------------------------------------------------
    ' Calls to access Retail Pro
    '------------------------------------------------------------------------------
    Public Shared Sub BOOpen(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BOOpen(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Opening Object")
    End Sub  'BOOpen

    Public Shared Sub BOClose(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BOClose(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "BOClose")
    End Sub  'BOClose

    Public Shared Sub BOEdit(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BOEdit(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Editing Object")
    End Sub  'BOEdit

    Public Shared Sub BOInsert(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BOInsert(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Inserting Object")
    End Sub  'BOInsert

    Public Shared Sub BODelete(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BODelete(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Deleting Object")
    End Sub  'BODelete

    Public Shared Sub BOPost(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BOPost(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Posting Object")
    End Sub  'BOPost

    Public Shared Sub BORefresh(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BORefresh(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Posting Object")
    End Sub  'BORefresh

    Public Shared Sub BORefreshRecord(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BORefreshRecord(objHandle, True, True)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Refreshing Object")
    End Sub  'BORefreshRecord

    Public Shared Sub BOPrior(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BOPrior(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "BOPrior")
    End Sub  'BOPrior

    Public Shared Sub BONext(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BONext(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "BONext")
    End Sub  'BONext

    Public Shared Sub BOFirst(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal errmsg As String)
        Dim retcode As Integer = adapter.BOFirst(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "BOFirst - " & errmsg)
    End Sub  'BOFirst

    Public Shared Sub BOLast(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer)
        Dim retcode As Integer = adapter.BOLast(objHandle)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "BOLast")
    End Sub  'BOLast

    Public Shared Function BOGetStrAttributeValueByName(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal attrName As String) As String
        Dim attr As Object = adapter.BOGetAttributeValueByName(objHandle, attrName)
        If attr.Equals(System.DBNull.Value) Then
            Return ""
        End If
        If attr Is Nothing Then
            MsgBox("Error retrieving attribute: " & attrName)
        End If
        Return CStr(attr)
    End Function  'BOGetStrAttributeValueByName - String

    Public Shared Function BOGetIntAttributeValueByName(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal attrName As String) As Integer
        Dim attr As Object = adapter.BOGetAttributeValueByName(objHandle, attrName)
        If attr.Equals(System.DBNull.Value) Then
            Return 0
        End If
        If attr Is Nothing Then
            MsgBox("Error retrieving attribute: " & attrName)
        End If
        Return CInt(attr)
    End Function  'BOGetIntAttributeValueByName - Int

    Public Shared Function BOGetDecAttributeValueByName(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal attrName As String) As Decimal
        Dim attr As Object = adapter.BOGetAttributeValueByName(objHandle, attrName)
        If attr.Equals(System.DBNull.Value) Then
            Return 0D
        End If
        If attr Is Nothing Then
            MsgBox("Error retrieving attribute: " & attrName)
        End If
        Return CDec(attr)
    End Function  'BOGetDecAttributeValueByName - Dec

    Public Shared Function BOGetBoolAttributeValueByName(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal attrName As String) As Boolean
        Dim attr As Object = adapter.BOGetAttributeValueByName(objHandle, attrName)
        If attr.Equals(System.DBNull.Value) Then
            Return False
        End If
        If attr Is Nothing Then
            MsgBox("Error retrieving attribute: " & attrName)
        End If
        Return CBool(attr)
    End Function  'BOGetBoolAttributeValueByName - Boolean

    Public Shared Sub BOSetAttributeValueByName(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal attrName As String, ByVal attrValue As String)
        Dim retcode As Integer = adapter.BOSetAttributeValueByName(objHandle, attrName, attrValue)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Setting attribute")
    End Sub  'BOSetAttributeValueByName - String

    Public Shared Sub BOSetAttributeValueByName(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal attrName As String, ByVal attrValue As Integer)
        Dim retcode As Integer = adapter.BOSetAttributeValueByName(objHandle, attrName, attrValue)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Setting attribute")
    End Sub  'BOSetAttributeValueByName - Integer

    Public Shared Sub BOSetAttributeValueByName(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal attrName As String, ByVal attrValue As Decimal)
        Dim retcode As Integer = adapter.BOSetAttributeValueByName(objHandle, attrName, attrValue)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Setting attribute")
    End Sub  'BOSetAttributeValueByName - Decimal

    Public Shared Sub BOSetCommitOnPost(ByRef adapter As RetailPro.Plugins.IPluginAdapter, ByRef objHandle As Integer, ByVal commit As Boolean)
        Dim retcode As Integer = adapter.BOSetCommitOnPost(objHandle, commit)
        If retcode <> RetailPro.Plugins.PluginError.peSuccess Then DisplayError(retcode, "Setting commit flag")
    End Sub  'BOSetCommitOnPost

    '------------------------------------------------------------------------------
    ' Displays error messages for calls to Retail Pro
    '------------------------------------------------------------------------------
    Public Shared Sub DisplayError(ByVal errNum As Integer, ByVal operation As String)
        Dim errMsg As String = "Unknown error type"
        Select Case errNum
            Case RetailPro.Plugins.PluginError.peGenericFailure
                errMsg = "Non-specific failure."
            Case RetailPro.Plugins.PluginError.peInvalidBOHandle
                errMsg = "The handle passed in was not valid for this plug-in."
            Case RetailPro.Plugins.PluginError.peSuccessNoResults
                errMsg = "The SQL statement processed without reporting error codes, but did not return a result set."
            Case RetailPro.Plugins.PluginError.peUnableToClose
                errMsg = "Call to BOClose failed. Either the business object wasn't open at that time " & vbCrLf & "or it could not be closed without first saving/canceling modifications."
            Case RetailPro.Plugins.PluginError.peUnableToCreateBO
                errMsg = "Unable to create business object."
            Case RetailPro.Plugins.PluginError.peUnableToDelete
                errMsg = "Call to BODelete failed. The business object may not have been open," & vbCrLf & "or the application may not allow deletions to the current record."
            Case RetailPro.Plugins.PluginError.peUnableToEdit
                errMsg = "Call to BOEdit failed."
            Case RetailPro.Plugins.PluginError.peUnableToInsert
                errMsg = "Call to BOInsert failed."
            Case RetailPro.Plugins.PluginError.peUnableToInsertIndex
                errMsg = "Unable to insert index"
            Case RetailPro.Plugins.PluginError.peUnableToInsertRecord
                errMsg = "Unable to insert record"
            Case RetailPro.Plugins.PluginError.peUnableToLocateByAttributes
                errMsg = "Call to BOLocateByAttributes failed."
            Case RetailPro.Plugins.PluginError.peUnableToOpen
                errMsg = "Call to BOOpen failed."
            Case RetailPro.Plugins.PluginError.peUnableToPost
                errMsg = "Call to BOPost failed." & vbCrLf & "The business object may have failed data validations," & vbCrLf & "or it may not have been in edit/insert mode."
            Case RetailPro.Plugins.PluginError.peUnableToReadFirstRow
                errMsg = "Unable to read first row."
            Case RetailPro.Plugins.PluginError.peUnableToReadLastRow
                errMsg = "Unable to read last row."
            Case RetailPro.Plugins.PluginError.peUnableToReadNextRow
                errMsg = "Unable to read next row."
            Case RetailPro.Plugins.PluginError.peUnableToRefresh
                errMsg = "Unable to refresh object."
            Case RetailPro.Plugins.PluginError.peUnableToSetAttributeValue
                errMsg = "Call to BOSetAttributeValue failed." & vbCrLf & "That attribute may not be intrinsically modifiable" & vbCrLf & "or you do not have write access to it."
            Case RetailPro.Plugins.PluginError.peUnsupportedBO
                errMsg = "The business object type specified is not supported."
            Case Else
                errMsg = "Return Code: " & errNum.ToString
        End Select
        MessageBox.Show(errMsg, operation)
    End Sub  'DisplayError

#End Region  'Methods

End Class  'Common

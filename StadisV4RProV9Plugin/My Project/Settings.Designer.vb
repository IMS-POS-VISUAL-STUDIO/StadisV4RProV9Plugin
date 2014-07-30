﻿'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.18444
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict On
Option Explicit On


Namespace My
    
    <Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute(),  _
     Global.System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "11.0.0.0"),  _
     Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)>  _
    Partial Friend NotInheritable Class MySettings
        Inherits Global.System.Configuration.ApplicationSettingsBase
        
        Private Shared defaultInstance As MySettings = CType(Global.System.Configuration.ApplicationSettingsBase.Synchronized(New MySettings()),MySettings)
        
#Region "My.Settings Auto-Save Functionality"
#If _MyType = "WindowsForms" Then
    Private Shared addedHandler As Boolean

    Private Shared addedHandlerLockObject As New Object

    <Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), Global.System.ComponentModel.EditorBrowsableAttribute(Global.System.ComponentModel.EditorBrowsableState.Advanced)> _
    Private Shared Sub AutoSaveSettings(ByVal sender As Global.System.Object, ByVal e As Global.System.EventArgs)
        If My.Application.SaveMySettingsOnExit Then
            My.Settings.Save()
        End If
    End Sub
#End If
#End Region
        
        Public Shared ReadOnly Property [Default]() As MySettings
            Get
                
#If _MyType = "WindowsForms" Then
               If Not addedHandler Then
                    SyncLock addedHandlerLockObject
                        If Not addedHandler Then
                            AddHandler My.Application.Shutdown, AddressOf AutoSaveSettings
                            addedHandler = True
                        End If
                    End SyncLock
                End If
#End If
                Return defaultInstance
            End Get
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("")>  _
        Public Property OverrideSettingID() As String
            Get
                Return CType(Me("OverrideSettingID"),String)
            End Get
            Set
                Me("OverrideSettingID") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("Test1")>  _
        Public Property StadisUserID() As String
            Get
                Return CType(Me("StadisUserID"),String)
            End Get
            Set
                Me("StadisUserID") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("password")>  _
        Public Property StadisPassword() As String
            Get
                Return CType(Me("StadisPassword"),String)
            End Get
            Set
                Me("StadisPassword") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("False")>  _
        Public Property Log() As Boolean
            Get
                Return CType(Me("Log"),Boolean)
            End Get
            Set
                Me("Log") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("False")>  _
        Public Property NetworkChecking() As Boolean
            Get
                Return CType(Me("NetworkChecking"),Boolean)
            End Get
            Set
                Me("NetworkChecking") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("Disabled")>  _
        Public Property OPOSPrinterName() As String
            Get
                Return CType(Me("OPOSPrinterName"),String)
            End Get
            Set
                Me("OPOSPrinterName") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("Disabled")>  _
        Public Property RasterPrinterName() As String
            Get
                Return CType(Me("RasterPrinterName"),String)
            End Get
            Set
                Me("RasterPrinterName") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("Disabled")>  _
        Public Property WindowsPrinterName() As String
            Get
                Return CType(Me("WindowsPrinterName"),String)
            End Get
            Set
                Me("WindowsPrinterName") = value
            End Set
        End Property
        
        <Global.System.Configuration.ApplicationScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.SpecialSettingAttribute(Global.System.Configuration.SpecialSetting.ConnectionString),  _
         Global.System.Configuration.DefaultSettingValueAttribute("Data Source=STADIS1;Initial Catalog=STADISV3;Integrated Security=True")>  _
        Public ReadOnly Property STADIS_V3ConnectionString() As String
            Get
                Return CType(Me("STADIS_V3ConnectionString"),String)
            End Get
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(), _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(), _
         Global.System.Configuration.DefaultSettingValueAttribute("http://192.168.100.14/StadisV4Web/StadisV4Transactions.asmx")> _
        Public Property StadisWebServiceURL() As String
            Get
                Return CType(Me("StadisWebServiceURL"), String)
            End Get
            Set(value As String)
                Me("StadisWebServiceURL") = Value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("")>  _
        Public Property SiteID() As String
            Get
                Return CType(Me("SiteID"),String)
            End Get
            Set
                Me("SiteID") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("")>  _
        Public Property VendorID() As String
            Get
                Return CType(Me("VendorID"),String)
            End Get
            Set
                Me("VendorID") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("")>  _
        Public Property LocationID() As String
            Get
                Return CType(Me("LocationID"),String)
            End Get
            Set
                Me("LocationID") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("")>  _
        Public Property RegisterID() As String
            Get
                Return CType(Me("RegisterID"),String)
            End Get
            Set
                Me("RegisterID") = value
            End Set
        End Property
        
        <Global.System.Configuration.UserScopedSettingAttribute(),  _
         Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
         Global.System.Configuration.DefaultSettingValueAttribute("")>  _
        Public Property CashierID() As String
            Get
                Return CType(Me("CashierID"),String)
            End Get
            Set
                Me("CashierID") = value
            End Set
        End Property
    End Class
End Namespace

Namespace My
    
    <Global.Microsoft.VisualBasic.HideModuleNameAttribute(),  _
     Global.System.Diagnostics.DebuggerNonUserCodeAttribute(),  _
     Global.System.Runtime.CompilerServices.CompilerGeneratedAttribute()>  _
    Friend Module MySettingsProperty
        
        <Global.System.ComponentModel.Design.HelpKeywordAttribute("My.Settings")>  _
        Friend ReadOnly Property Settings() As Global.StadisV4RProV9Plugin.My.MySettings
            Get
                Return Global.StadisV4RProV9Plugin.My.MySettings.Default
            End Get
        End Property
    End Module
End Namespace

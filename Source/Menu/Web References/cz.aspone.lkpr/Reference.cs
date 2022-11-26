﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Tento kód byl generován nástrojem.
//     Verze modulu runtime:4.0.30319.42000
//
//     Změny tohoto souboru mohou způsobit nesprávné chování a budou ztraceny,
//     dojde-li k novému generování kódu.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// Tento zdrojový kód byl automaticky vytvořen Microsoft.VSDesigner, Verze 4.0.30319.42000.
// 
#pragma warning disable 1591

namespace ORTS.cz.aspone.lkpr {
    using System.Diagnostics;
    using System;
    using System.Xml.Serialization;
    using System.ComponentModel;
    using System.Web.Services.Protocols;
    using System.Web.Services;
    using System.Data;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Web.Services.WebServiceBindingAttribute(Name="WebServiceSoap", Namespace="http://tempuri.org/")]
    public partial class WebService : System.Web.Services.Protocols.SoapHttpClientProtocol {
        
        private System.Threading.SendOrPostCallback GetMirelSignalsOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetPowerSupplyStationsOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetPowerSupplyMarkersOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetLastVersionOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetPowerSuplyStationVersionOperationCompleted;
        
        private System.Threading.SendOrPostCallback GetPowerSuplyMarkerVersionOperationCompleted;
        
        private System.Threading.SendOrPostCallback SaveMirelSignalOperationCompleted;
        
        private System.Threading.SendOrPostCallback SavePowerSupplyStationOperationCompleted;
        
        private System.Threading.SendOrPostCallback SavePowerSupplyMarkerOperationCompleted;
        
        private System.Threading.SendOrPostCallback UpdateMirelVersionOperationCompleted;
        
        private System.Threading.SendOrPostCallback UpdatePowerSuplyStationVersionOperationCompleted;
        
        private System.Threading.SendOrPostCallback UpdatePowerSupplyMarkerVersionOperationCompleted;
        
        private bool useDefaultCredentialsSetExplicitly;
        
        /// <remarks/>
        public WebService() {
            this.Url = global::ORTS.Properties.Settings.Default.Menu_cz_aspone_lkpr_WebService;
            if ((this.IsLocalFileSystemWebService(this.Url) == true)) {
                this.UseDefaultCredentials = true;
                this.useDefaultCredentialsSetExplicitly = false;
            }
            else {
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        public new string Url {
            get {
                return base.Url;
            }
            set {
                if ((((this.IsLocalFileSystemWebService(base.Url) == true) 
                            && (this.useDefaultCredentialsSetExplicitly == false)) 
                            && (this.IsLocalFileSystemWebService(value) == false))) {
                    base.UseDefaultCredentials = false;
                }
                base.Url = value;
            }
        }
        
        public new bool UseDefaultCredentials {
            get {
                return base.UseDefaultCredentials;
            }
            set {
                base.UseDefaultCredentials = value;
                this.useDefaultCredentialsSetExplicitly = true;
            }
        }
        
        /// <remarks/>
        public event GetMirelSignalsCompletedEventHandler GetMirelSignalsCompleted;
        
        /// <remarks/>
        public event GetPowerSupplyStationsCompletedEventHandler GetPowerSupplyStationsCompleted;
        
        /// <remarks/>
        public event GetPowerSupplyMarkersCompletedEventHandler GetPowerSupplyMarkersCompleted;
        
        /// <remarks/>
        public event GetLastVersionCompletedEventHandler GetLastVersionCompleted;
        
        /// <remarks/>
        public event GetPowerSuplyStationVersionCompletedEventHandler GetPowerSuplyStationVersionCompleted;
        
        /// <remarks/>
        public event GetPowerSuplyMarkerVersionCompletedEventHandler GetPowerSuplyMarkerVersionCompleted;
        
        /// <remarks/>
        public event SaveMirelSignalCompletedEventHandler SaveMirelSignalCompleted;
        
        /// <remarks/>
        public event SavePowerSupplyStationCompletedEventHandler SavePowerSupplyStationCompleted;
        
        /// <remarks/>
        public event SavePowerSupplyMarkerCompletedEventHandler SavePowerSupplyMarkerCompleted;
        
        /// <remarks/>
        public event UpdateMirelVersionCompletedEventHandler UpdateMirelVersionCompleted;
        
        /// <remarks/>
        public event UpdatePowerSuplyStationVersionCompletedEventHandler UpdatePowerSuplyStationVersionCompleted;
        
        /// <remarks/>
        public event UpdatePowerSupplyMarkerVersionCompletedEventHandler UpdatePowerSupplyMarkerVersionCompleted;
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetMirelSignals", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public System.Data.DataTable GetMirelSignals(string TrackName, string Version) {
            object[] results = this.Invoke("GetMirelSignals", new object[] {
                        TrackName,
                        Version});
            return ((System.Data.DataTable)(results[0]));
        }
        
        /// <remarks/>
        public void GetMirelSignalsAsync(string TrackName, string Version) {
            this.GetMirelSignalsAsync(TrackName, Version, null);
        }
        
        /// <remarks/>
        public void GetMirelSignalsAsync(string TrackName, string Version, object userState) {
            if ((this.GetMirelSignalsOperationCompleted == null)) {
                this.GetMirelSignalsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetMirelSignalsOperationCompleted);
            }
            this.InvokeAsync("GetMirelSignals", new object[] {
                        TrackName,
                        Version}, this.GetMirelSignalsOperationCompleted, userState);
        }
        
        private void OnGetMirelSignalsOperationCompleted(object arg) {
            if ((this.GetMirelSignalsCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetMirelSignalsCompleted(this, new GetMirelSignalsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetPowerSupplyStations", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public System.Data.DataTable GetPowerSupplyStations(string TrackName, string Version) {
            object[] results = this.Invoke("GetPowerSupplyStations", new object[] {
                        TrackName,
                        Version});
            return ((System.Data.DataTable)(results[0]));
        }
        
        /// <remarks/>
        public void GetPowerSupplyStationsAsync(string TrackName, string Version) {
            this.GetPowerSupplyStationsAsync(TrackName, Version, null);
        }
        
        /// <remarks/>
        public void GetPowerSupplyStationsAsync(string TrackName, string Version, object userState) {
            if ((this.GetPowerSupplyStationsOperationCompleted == null)) {
                this.GetPowerSupplyStationsOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetPowerSupplyStationsOperationCompleted);
            }
            this.InvokeAsync("GetPowerSupplyStations", new object[] {
                        TrackName,
                        Version}, this.GetPowerSupplyStationsOperationCompleted, userState);
        }
        
        private void OnGetPowerSupplyStationsOperationCompleted(object arg) {
            if ((this.GetPowerSupplyStationsCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetPowerSupplyStationsCompleted(this, new GetPowerSupplyStationsCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetPowerSupplyMarkers", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public System.Data.DataTable GetPowerSupplyMarkers(string TrackName, string Version) {
            object[] results = this.Invoke("GetPowerSupplyMarkers", new object[] {
                        TrackName,
                        Version});
            return ((System.Data.DataTable)(results[0]));
        }
        
        /// <remarks/>
        public void GetPowerSupplyMarkersAsync(string TrackName, string Version) {
            this.GetPowerSupplyMarkersAsync(TrackName, Version, null);
        }
        
        /// <remarks/>
        public void GetPowerSupplyMarkersAsync(string TrackName, string Version, object userState) {
            if ((this.GetPowerSupplyMarkersOperationCompleted == null)) {
                this.GetPowerSupplyMarkersOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetPowerSupplyMarkersOperationCompleted);
            }
            this.InvokeAsync("GetPowerSupplyMarkers", new object[] {
                        TrackName,
                        Version}, this.GetPowerSupplyMarkersOperationCompleted, userState);
        }
        
        private void OnGetPowerSupplyMarkersOperationCompleted(object arg) {
            if ((this.GetPowerSupplyMarkersCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetPowerSupplyMarkersCompleted(this, new GetPowerSupplyMarkersCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetLastVersion", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string GetLastVersion(string Track) {
            object[] results = this.Invoke("GetLastVersion", new object[] {
                        Track});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void GetLastVersionAsync(string Track) {
            this.GetLastVersionAsync(Track, null);
        }
        
        /// <remarks/>
        public void GetLastVersionAsync(string Track, object userState) {
            if ((this.GetLastVersionOperationCompleted == null)) {
                this.GetLastVersionOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetLastVersionOperationCompleted);
            }
            this.InvokeAsync("GetLastVersion", new object[] {
                        Track}, this.GetLastVersionOperationCompleted, userState);
        }
        
        private void OnGetLastVersionOperationCompleted(object arg) {
            if ((this.GetLastVersionCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetLastVersionCompleted(this, new GetLastVersionCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetPowerSuplyStationVersion", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string GetPowerSuplyStationVersion(string Track) {
            object[] results = this.Invoke("GetPowerSuplyStationVersion", new object[] {
                        Track});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void GetPowerSuplyStationVersionAsync(string Track) {
            this.GetPowerSuplyStationVersionAsync(Track, null);
        }
        
        /// <remarks/>
        public void GetPowerSuplyStationVersionAsync(string Track, object userState) {
            if ((this.GetPowerSuplyStationVersionOperationCompleted == null)) {
                this.GetPowerSuplyStationVersionOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetPowerSuplyStationVersionOperationCompleted);
            }
            this.InvokeAsync("GetPowerSuplyStationVersion", new object[] {
                        Track}, this.GetPowerSuplyStationVersionOperationCompleted, userState);
        }
        
        private void OnGetPowerSuplyStationVersionOperationCompleted(object arg) {
            if ((this.GetPowerSuplyStationVersionCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetPowerSuplyStationVersionCompleted(this, new GetPowerSuplyStationVersionCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/GetPowerSuplyMarkerVersion", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public string GetPowerSuplyMarkerVersion(string Track) {
            object[] results = this.Invoke("GetPowerSuplyMarkerVersion", new object[] {
                        Track});
            return ((string)(results[0]));
        }
        
        /// <remarks/>
        public void GetPowerSuplyMarkerVersionAsync(string Track) {
            this.GetPowerSuplyMarkerVersionAsync(Track, null);
        }
        
        /// <remarks/>
        public void GetPowerSuplyMarkerVersionAsync(string Track, object userState) {
            if ((this.GetPowerSuplyMarkerVersionOperationCompleted == null)) {
                this.GetPowerSuplyMarkerVersionOperationCompleted = new System.Threading.SendOrPostCallback(this.OnGetPowerSuplyMarkerVersionOperationCompleted);
            }
            this.InvokeAsync("GetPowerSuplyMarkerVersion", new object[] {
                        Track}, this.GetPowerSuplyMarkerVersionOperationCompleted, userState);
        }
        
        private void OnGetPowerSuplyMarkerVersionOperationCompleted(object arg) {
            if ((this.GetPowerSuplyMarkerVersionCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.GetPowerSuplyMarkerVersionCompleted(this, new GetPowerSuplyMarkerVersionCompletedEventArgs(invokeArgs.Results, invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/SaveMirelSignal", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void SaveMirelSignal(string TrackName, int SectionID, string MirelState, int DatabaseVersion) {
            this.Invoke("SaveMirelSignal", new object[] {
                        TrackName,
                        SectionID,
                        MirelState,
                        DatabaseVersion});
        }
        
        /// <remarks/>
        public void SaveMirelSignalAsync(string TrackName, int SectionID, string MirelState, int DatabaseVersion) {
            this.SaveMirelSignalAsync(TrackName, SectionID, MirelState, DatabaseVersion, null);
        }
        
        /// <remarks/>
        public void SaveMirelSignalAsync(string TrackName, int SectionID, string MirelState, int DatabaseVersion, object userState) {
            if ((this.SaveMirelSignalOperationCompleted == null)) {
                this.SaveMirelSignalOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSaveMirelSignalOperationCompleted);
            }
            this.InvokeAsync("SaveMirelSignal", new object[] {
                        TrackName,
                        SectionID,
                        MirelState,
                        DatabaseVersion}, this.SaveMirelSignalOperationCompleted, userState);
        }
        
        private void OnSaveMirelSignalOperationCompleted(object arg) {
            if ((this.SaveMirelSignalCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SaveMirelSignalCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/SavePowerSupplyStation", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void SavePowerSupplyStation(int Id, string TrackName, string Lat, string Lon, string PowerSystem, int DatabaseVersion) {
            this.Invoke("SavePowerSupplyStation", new object[] {
                        Id,
                        TrackName,
                        Lat,
                        Lon,
                        PowerSystem,
                        DatabaseVersion});
        }
        
        /// <remarks/>
        public void SavePowerSupplyStationAsync(int Id, string TrackName, string Lat, string Lon, string PowerSystem, int DatabaseVersion) {
            this.SavePowerSupplyStationAsync(Id, TrackName, Lat, Lon, PowerSystem, DatabaseVersion, null);
        }
        
        /// <remarks/>
        public void SavePowerSupplyStationAsync(int Id, string TrackName, string Lat, string Lon, string PowerSystem, int DatabaseVersion, object userState) {
            if ((this.SavePowerSupplyStationOperationCompleted == null)) {
                this.SavePowerSupplyStationOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSavePowerSupplyStationOperationCompleted);
            }
            this.InvokeAsync("SavePowerSupplyStation", new object[] {
                        Id,
                        TrackName,
                        Lat,
                        Lon,
                        PowerSystem,
                        DatabaseVersion}, this.SavePowerSupplyStationOperationCompleted, userState);
        }
        
        private void OnSavePowerSupplyStationOperationCompleted(object arg) {
            if ((this.SavePowerSupplyStationCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SavePowerSupplyStationCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/SavePowerSupplyMarker", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void SavePowerSupplyMarker(int Id, string TrackName, string Lat, string Lon, int Voltage, int DatabaseVersion) {
            this.Invoke("SavePowerSupplyMarker", new object[] {
                        Id,
                        TrackName,
                        Lat,
                        Lon,
                        Voltage,
                        DatabaseVersion});
        }
        
        /// <remarks/>
        public void SavePowerSupplyMarkerAsync(int Id, string TrackName, string Lat, string Lon, int Voltage, int DatabaseVersion) {
            this.SavePowerSupplyMarkerAsync(Id, TrackName, Lat, Lon, Voltage, DatabaseVersion, null);
        }
        
        /// <remarks/>
        public void SavePowerSupplyMarkerAsync(int Id, string TrackName, string Lat, string Lon, int Voltage, int DatabaseVersion, object userState) {
            if ((this.SavePowerSupplyMarkerOperationCompleted == null)) {
                this.SavePowerSupplyMarkerOperationCompleted = new System.Threading.SendOrPostCallback(this.OnSavePowerSupplyMarkerOperationCompleted);
            }
            this.InvokeAsync("SavePowerSupplyMarker", new object[] {
                        Id,
                        TrackName,
                        Lat,
                        Lon,
                        Voltage,
                        DatabaseVersion}, this.SavePowerSupplyMarkerOperationCompleted, userState);
        }
        
        private void OnSavePowerSupplyMarkerOperationCompleted(object arg) {
            if ((this.SavePowerSupplyMarkerCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.SavePowerSupplyMarkerCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/UpdateMirelVersion", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void UpdateMirelVersion(int Version, string Track) {
            this.Invoke("UpdateMirelVersion", new object[] {
                        Version,
                        Track});
        }
        
        /// <remarks/>
        public void UpdateMirelVersionAsync(int Version, string Track) {
            this.UpdateMirelVersionAsync(Version, Track, null);
        }
        
        /// <remarks/>
        public void UpdateMirelVersionAsync(int Version, string Track, object userState) {
            if ((this.UpdateMirelVersionOperationCompleted == null)) {
                this.UpdateMirelVersionOperationCompleted = new System.Threading.SendOrPostCallback(this.OnUpdateMirelVersionOperationCompleted);
            }
            this.InvokeAsync("UpdateMirelVersion", new object[] {
                        Version,
                        Track}, this.UpdateMirelVersionOperationCompleted, userState);
        }
        
        private void OnUpdateMirelVersionOperationCompleted(object arg) {
            if ((this.UpdateMirelVersionCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.UpdateMirelVersionCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/UpdatePowerSuplyStationVersion", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void UpdatePowerSuplyStationVersion(int Version, string Track) {
            this.Invoke("UpdatePowerSuplyStationVersion", new object[] {
                        Version,
                        Track});
        }
        
        /// <remarks/>
        public void UpdatePowerSuplyStationVersionAsync(int Version, string Track) {
            this.UpdatePowerSuplyStationVersionAsync(Version, Track, null);
        }
        
        /// <remarks/>
        public void UpdatePowerSuplyStationVersionAsync(int Version, string Track, object userState) {
            if ((this.UpdatePowerSuplyStationVersionOperationCompleted == null)) {
                this.UpdatePowerSuplyStationVersionOperationCompleted = new System.Threading.SendOrPostCallback(this.OnUpdatePowerSuplyStationVersionOperationCompleted);
            }
            this.InvokeAsync("UpdatePowerSuplyStationVersion", new object[] {
                        Version,
                        Track}, this.UpdatePowerSuplyStationVersionOperationCompleted, userState);
        }
        
        private void OnUpdatePowerSuplyStationVersionOperationCompleted(object arg) {
            if ((this.UpdatePowerSuplyStationVersionCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.UpdatePowerSuplyStationVersionCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        [System.Web.Services.Protocols.SoapDocumentMethodAttribute("http://tempuri.org/UpdatePowerSupplyMarkerVersion", RequestNamespace="http://tempuri.org/", ResponseNamespace="http://tempuri.org/", Use=System.Web.Services.Description.SoapBindingUse.Literal, ParameterStyle=System.Web.Services.Protocols.SoapParameterStyle.Wrapped)]
        public void UpdatePowerSupplyMarkerVersion(int Version, string Track) {
            this.Invoke("UpdatePowerSupplyMarkerVersion", new object[] {
                        Version,
                        Track});
        }
        
        /// <remarks/>
        public void UpdatePowerSupplyMarkerVersionAsync(int Version, string Track) {
            this.UpdatePowerSupplyMarkerVersionAsync(Version, Track, null);
        }
        
        /// <remarks/>
        public void UpdatePowerSupplyMarkerVersionAsync(int Version, string Track, object userState) {
            if ((this.UpdatePowerSupplyMarkerVersionOperationCompleted == null)) {
                this.UpdatePowerSupplyMarkerVersionOperationCompleted = new System.Threading.SendOrPostCallback(this.OnUpdatePowerSupplyMarkerVersionOperationCompleted);
            }
            this.InvokeAsync("UpdatePowerSupplyMarkerVersion", new object[] {
                        Version,
                        Track}, this.UpdatePowerSupplyMarkerVersionOperationCompleted, userState);
        }
        
        private void OnUpdatePowerSupplyMarkerVersionOperationCompleted(object arg) {
            if ((this.UpdatePowerSupplyMarkerVersionCompleted != null)) {
                System.Web.Services.Protocols.InvokeCompletedEventArgs invokeArgs = ((System.Web.Services.Protocols.InvokeCompletedEventArgs)(arg));
                this.UpdatePowerSupplyMarkerVersionCompleted(this, new System.ComponentModel.AsyncCompletedEventArgs(invokeArgs.Error, invokeArgs.Cancelled, invokeArgs.UserState));
            }
        }
        
        /// <remarks/>
        public new void CancelAsync(object userState) {
            base.CancelAsync(userState);
        }
        
        private bool IsLocalFileSystemWebService(string url) {
            if (((url == null) 
                        || (url == string.Empty))) {
                return false;
            }
            System.Uri wsUri = new System.Uri(url);
            if (((wsUri.Port >= 1024) 
                        && (string.Compare(wsUri.Host, "localHost", System.StringComparison.OrdinalIgnoreCase) == 0))) {
                return true;
            }
            return false;
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void GetMirelSignalsCompletedEventHandler(object sender, GetMirelSignalsCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetMirelSignalsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetMirelSignalsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public System.Data.DataTable Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((System.Data.DataTable)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void GetPowerSupplyStationsCompletedEventHandler(object sender, GetPowerSupplyStationsCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetPowerSupplyStationsCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetPowerSupplyStationsCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public System.Data.DataTable Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((System.Data.DataTable)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void GetPowerSupplyMarkersCompletedEventHandler(object sender, GetPowerSupplyMarkersCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetPowerSupplyMarkersCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetPowerSupplyMarkersCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public System.Data.DataTable Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((System.Data.DataTable)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void GetLastVersionCompletedEventHandler(object sender, GetLastVersionCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetLastVersionCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetLastVersionCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void GetPowerSuplyStationVersionCompletedEventHandler(object sender, GetPowerSuplyStationVersionCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetPowerSuplyStationVersionCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetPowerSuplyStationVersionCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void GetPowerSuplyMarkerVersionCompletedEventHandler(object sender, GetPowerSuplyMarkerVersionCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class GetPowerSuplyMarkerVersionCompletedEventArgs : System.ComponentModel.AsyncCompletedEventArgs {
        
        private object[] results;
        
        internal GetPowerSuplyMarkerVersionCompletedEventArgs(object[] results, System.Exception exception, bool cancelled, object userState) : 
                base(exception, cancelled, userState) {
            this.results = results;
        }
        
        /// <remarks/>
        public string Result {
            get {
                this.RaiseExceptionIfNecessary();
                return ((string)(this.results[0]));
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void SaveMirelSignalCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void SavePowerSupplyStationCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void SavePowerSupplyMarkerCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void UpdateMirelVersionCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void UpdatePowerSuplyStationVersionCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Web.Services", "4.8.9032.0")]
    public delegate void UpdatePowerSupplyMarkerVersionCompletedEventHandler(object sender, System.ComponentModel.AsyncCompletedEventArgs e);
}

#pragma warning restore 1591
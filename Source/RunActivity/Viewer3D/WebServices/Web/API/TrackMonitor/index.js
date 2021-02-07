// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.
//
// Based on original work by Dan Reynolds 2017-12-21

// Using XMLHttpRequest rather than fetch() as:
// 1. it is more widely supported (e.g. Internet Explorer and various tablets)
// 2. It doesn't hide some returning error codes
// 3. We don't need the ability to chain promises that fetch() offers.

var hr = new XMLHttpRequest;
var httpCodeSuccess = 200;
var xmlHttpRequestCodeDone = 4;

var idleMs = 500; // default idle time between calls
function poll(initialIdleMs) {
	if (initialIdleMs != null){
		idleMs = initialIdleMs; // Save it to use at end
	}
	api();
	
	// setTimeout() used instead of setInterval() to avoid overloading the browser's queue.
	// (It's not true recursion, so it won't blow the stack.)
    setTimeout(poll, idleMs); // In this call, initialIdleMs == null
}

function api() {
	// If this file is located in folder /API/<API_name>/, then Open Rails will call the API with the signature "/API/<API_name"

	// GET preferred over POST as Internet Explorer may then fail intermittently with network error 00002eff
	// hr.open("post", "call_API", true);
	// hr.send(""");
	hr.open("GET", "call_API", true);
	hr.send();
	
	hr.onreadystatechange = function () {
		if (this.readyState == xmlHttpRequestCodeDone && this.status == httpCodeSuccess) {
			var obj = JSON.parse(hr.responseText);
			if (obj != null) // Can happen using IEv11
			{
                Str = "<table>";
				var endIndexFirst = 0,
					endIndexTrackLeft = 0,
					endIndexTrack = 0,
					endIndexTrackRight = 0,
					endIndexLimit = 0,
					endIndexSignal = 0,
					endIndexDist = 0;

				var newDataFirst = "",
					newDataTrack = "",
					newDataLimit = "",
					newDataSignal = "",
					newDataDist = "",
					stringColorFirst = "",
					stringColorTrackLeft = "",
					stringColorTrack = "",
					stringColorTrackRight = "",
					stringColorLimit = "",
					stringColorSignal = "",
					stringColorDist = "";

				// Color codes
				var codeColor = ['???','??!','?!?','?!!','!??','!!?','!!!','%%%','%$$','%%$','$%$','$$$'];

				//controlMode
				var modes = ["AUTO_SIGNAL", "AUTO_NODE", "MANUAL", "EXPLORER", "OUT_OF_CONTROL", "INACTIVE", "TURNTABLE", "UNDEFINED"];
				var controlMode = modes[obj.controlMode];

				// Table title
				Str += "<tr> <td colspan='9' style='text-align: center'>" + 'Track Monitor' + "</td></tr>";
				Str += "<tr> <td colspan='9' class='separator'></td></tr>";
				// Customize data
				for (var row = 0; row < obj.trackMonitorData.length; ++row) {
					Str += "<tr>";
					firstColor = false;
					trackColorLeft = false;
					trackColor = false;
					trackColorRight = false;
					limitColor = false;
					signalColor = false;
					distColor = false;

					// FirstCol
					if (obj.trackMonitorData[row].FirstCol.length > 0) {
						endIndexFirst = obj.trackMonitorData[row].FirstCol.length;
						newDataFirst = obj.trackMonitorData[row].FirstCol.slice(0, endIndexFirst -3);
						stringColorFirst = obj.trackMonitorData[row].FirstCol.slice(-3);
					}
					// TrackColLeft
					if (obj.trackMonitorData[row].TrackColLeft.length > 0) {
						endIndexTrackLeft = obj.trackMonitorData[row].TrackColLeft.length;
						newDataTrackLeft = obj.trackMonitorData[row].TrackColLeft.slice(0, endIndexTrackLeft -3);
						stringColorTrackLeft = obj.trackMonitorData[row].TrackColLeft.slice(-3);
					}
					// TrackCol
					if (obj.trackMonitorData[row].TrackCol.length > 0) {
						endIndexTrack = obj.trackMonitorData[row].TrackCol.length;
						newDataTrack = obj.trackMonitorData[row].TrackCol.slice(0, endIndexTrack -3);
						stringColorTrack = obj.trackMonitorData[row].TrackCol.slice(-3);
					}
					// TrackColRight
					if (obj.trackMonitorData[row].TrackColRight.length > 0) {
						endIndexTrackRight = obj.trackMonitorData[row].TrackColRight.length;
						newDataTrackRight = obj.trackMonitorData[row].TrackColRight.slice(0, endIndexTrackRight -3);
						stringColorTrackRight = obj.trackMonitorData[row].TrackColRight.slice(-3);
					}
					// LimitCol
					if (obj.trackMonitorData[row].LimitCol.length > 0) {
						endIndexLimit = obj.trackMonitorData[row].LimitCol.length;
						newDataLimit = obj.trackMonitorData[row].LimitCol.slice(0, endIndexLimit -3);
						stringColorLimit = obj.trackMonitorData[row].LimitCol.slice(-3);
					}
					// SignalCol
					if (obj.trackMonitorData[row].SignalCol.length > 0) {
						endIndexSignal = obj.trackMonitorData[row].SignalCol.length;
						newDataSignal = obj.trackMonitorData[row].SignalCol.slice(0, endIndexSignal -3);
						stringColorSignal = obj.trackMonitorData[row].SignalCol.slice(-3);
					}
					// DistCol
					if (obj.trackMonitorData[row].DistCol.length > 0) {
						endIndexDist = obj.trackMonitorData[row].DistCol.length;
						newDataDist = obj.trackMonitorData[row].DistCol.slice(0, endIndexDist -3);
						stringColorDist = obj.trackMonitorData[row].DistCol.slice(-3);
					}

					// detects color
					if (codeColor.indexOf(stringColorFirst) != -1) { firstColor = true; }
					if (codeColor.indexOf(stringColorTrackLeft) != -1) { trackColorLeft = true; }
					if (codeColor.indexOf(stringColorTrack) != -1) { trackColor = true; }
					if (codeColor.indexOf(stringColorTrackRight) != -1) { trackColorRight = true; }
					if (codeColor.indexOf(stringColorLimit) != -1) { limitColor = true; }
					if (codeColor.indexOf(stringColorSignal) != -1) { signalColor = true; }
					if (codeColor.indexOf(stringColorDist) != -1) { distColor = true; }

					if (obj.trackMonitorData[row].FirstCol == null) {
						Str += "<td colspan='2'></td>";
					}
					else if (obj.trackMonitorData[row].FirstCol == "Sprtr"){
						Str += "<td colspan='9' class='separator'></td>";
					}
					else if (obj.trackMonitorData[row].FirstCol == "SprtrRed"){
						Str += "<td colspan='9' class='separatorred'></td>";
					}
					else if (obj.trackMonitorData[row].FirstCol == "SprtrDarkGray"){
						Str += "<td colspan='9' class='separatordarkgray'></td>";
					}
					else if (row == 9 ){
						Str += "<td colspan='9' align='center' >" + obj.trackMonitorData[row].FirstCol + "</td>";
					}
					else{
						if (row < 8){
							// first col = FirstCol data
							DisplayItem('left', 3, firstColor, stringColorFirst, firstColor? newDataFirst : obj.trackMonitorData[row].FirstCol, false);
							Str += "<td></td>";
							Str += "<td></td>";

							// third col = TrackCol data
							DisplayItem('right', 3, trackColor, stringColorTrack, trackColor? newDataTrack : obj.trackMonitorData[row].TrackCol, false);
						}
						else{
							// first col = FirstCol data
							if (row > 25 && controlMode.indexOf("AUTO") != -1){
								DisplayItem('center', 1, firstColor, stringColorFirst, firstColor? newDataFirst : obj.trackMonitorData[row].FirstCol, true );
							}
							else{
								DisplayItem('left', 1, firstColor, stringColorFirst, firstColor? newDataFirst : obj.trackMonitorData[row].FirstCol, false );
							}

							// second col = TrackColLeft data
							DisplayItem('right', 1, trackColorLeft, stringColorTrackLeft, trackColorLeft? newDataTrackLeft : obj.trackMonitorData[row].TrackColLeft, false);

							// third col = TrackCol data
							DisplayItem('center', 2, trackColor, stringColorTrack, trackColor? newDataTrack : obj.trackMonitorData[row].TrackCol, false);

							// fourth col = TrackColRight data
							DisplayItem('left', 1, trackColorRight, stringColorTrackRight, trackColorRight? newDataTrackRight : obj.trackMonitorData[row].TrackColRight, false);

							// station zone
							if (row > 25 && controlMode.indexOf("AUTO") != -1){
								// fifth col = LimitCol data
								DisplayItem('left', 3, limitColor, stringColorLimit, limitColor? newDataLimit : obj.trackMonitorData[row].LimitCol, true);
							}
							else{
								// fifth col = LimitCol data
								DisplayItem('left', 1, limitColor, stringColorLimit, limitColor? newDataLimit : obj.trackMonitorData[row].LimitCol, false);

								// sixth col = SignalCol data
								DisplayItem('center', 1, signalColor, stringColorSignal, signalColor? newDataSignal : obj.trackMonitorData[row].SignalCol, false);

								// seventh col = DistCol data
								DisplayItem('right', 1, distColor, stringColorDist, distColor? newDataDist:obj.trackMonitorData[row].DistCol, false);
							}
						}
					}
					Str += "</tr>";
				}
				Str += "</table>";
				TrackMonitor.innerHTML = Str;
            }
		}
	}
}

function DisplayItem(alignement, colspanvalue, isColor, colorCode, item, small){
	Str += "<td align='" + alignement + "' colspan='" + colspanvalue + "' ColorCode=" + (isColor? colorCode : '') + ">"  + (small? item.small() : item) + "</td>";
}

function changePageColor() {
	var buttonClicked = document.getElementById("buttonDN");
	var bodyColor = document.getElementById("body");

	if (buttonClicked.innerHTML == "Night"){
		buttonClicked.innerHTML = "Day";
		bodyColor.style.background = "black";
		bodyColor.style.color =	"white";
	}
	else if (buttonClicked.innerHTML == "Day"){
		buttonClicked.innerHTML = "Night";
		bodyColor.style.background = "white";
		bodyColor.style.color =	"black";
	}
};
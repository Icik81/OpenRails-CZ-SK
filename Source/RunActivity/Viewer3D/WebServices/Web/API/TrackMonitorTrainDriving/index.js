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
	apiTM();

	// setTimeout() used instead of setInterval() to avoid overloading the browser's queue.
	// (It's not true recursion, so it won't blow the stack.)
    setTimeout(poll, idleMs); // In this call, initialIdleMs == null
}

function apiTM() {
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
					stringColorLimit = "";
					stringColorSignal = "";
					stringColorDist = "";

				var	autoMode = false;

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

				Str = "<table>";
				var endIndexFirst = 0,
					endIndexLimit = 0,
					endIndexKey = 0;

				var keyPressedColor = "",
					newDataFirst = "",
					newDataLimit = "",
					smallSymbolColor = "",
					stringColorFirst = "",
					stringColorLimit = "";
				// Table title
				Str += "<tr> <td colspan='5' style='text-align: center'>" + 'Train Driving Info' + "</td></tr>";
				Str += "<tr> <td colspan='5' class='separator'></td></tr>";

				// Customize data
				for (var row = 0; row < obj.trainDrivingData.length; ++row) {
					Str += "<tr>";
					firstColor = false;
					lastColor = false;
					keyColor = false;
					symbolColor = false;

					// FirstCol
					if (obj.trainDrivingData[row].FirstCol != null) {
						endIndexFirst = obj.trainDrivingData[row].FirstCol.length;
						newDataFirst = obj.trainDrivingData[row].FirstCol.slice(0, endIndexFirst -3);
						stringColorFirst = obj.trainDrivingData[row].FirstCol.slice(-3);
					}

					// LastCol
					if (obj.trainDrivingData[row].LastCol != null) {
						endIndexLimit = obj.trainDrivingData[row].LastCol.length;
						newDataLimit = obj.trainDrivingData[row].LastCol.slice(0, endIndexLimit -3);
						stringColorLimit = obj.trainDrivingData[row].LastCol.slice(-3);
					}

					// keyPressed
					if (obj.trainDrivingData[row].keyPressed != null) {
						endIndexKey = obj.trainDrivingData[row].keyPressed.length;
						newDataKey = obj.trainDrivingData[row].keyPressed.slice(0, endIndexKey -3);
						keyPressedColor = obj.trainDrivingData[row].keyPressed.slice(-3);
					}

					// smallSymbol
					if (obj.trainDrivingData[row].SymbolCol != null) {
						endIndexSymbol = obj.trainDrivingData[row].SymbolCol.length;
						newDataSymbol = obj.trainDrivingData[row].SymbolCol.slice(0, endIndexSymbol -3);
						smallSymbolColor = obj.trainDrivingData[row].SymbolCol.slice(-3);
					}

					// detects color
					if (codeColor.indexOf(stringColorFirst) != -1) { firstColor = true; }
					if (codeColor.indexOf(stringColorLimit) != -1) { lastColor = true; }
					if (codeColor.indexOf(keyPressedColor) != -1) { keyColor = true; }
					if (codeColor.indexOf(smallSymbolColor) != -1) { symbolColor = true; }

					if (obj.trainDrivingData[row].FirstCol == null) {
						Str += "<td></td>";
					}
					else if (obj.trainDrivingData[row].FirstCol == "Sprtr"){
						Str += "<td colspan='5' class='separator'></td>";
					}
					else{
						// first col  = key symbol
						if (keyColor == true){
							Str += "<td ColorCode=" + keyPressedColor + ">" + newDataKey + "</td>";
						}
						else{
							Str += "<td width='16'>" + obj.trainDrivingData[row].keyPressed + "</td>";
						}

						// second col = FirstCol data
						if(firstColor == true){
							Str += "<td ColorCode=" + stringColorFirst + ">" + newDataFirst + "</td>";
						}
						else{
							Str += "<td>" + obj.trainDrivingData[row].FirstCol + "</td>";
						}

						// third col  = key symbol
						if (keyColor == true){
							Str += "<td ColorCode=" + keyPressedColor + ">" + newDataKey + "</td>";
						}
						else if (symbolColor == true){
							Str += "<td ColorCode=" + smallSymbolColor + ">" + newDataSymbol + "</td>";
						}
						else{
							Str += "<td width='16'>" + obj.trainDrivingData[row].keyPressed + "</td>";
						}

						// fourth col = LastCol data
						if(lastColor == true){
							Str += "<td ColorCode=" + stringColorLimit + ">" + newDataLimit + "</td>";
						}
						else{
							Str += "<td>" + obj.trainDrivingData[row].LastCol + "</td>";
						}
					}
					Str += "</tr>";
				}
				// space at bottom
				Str += "<tr> <td colspan='5' style='text-align: center'>" + '.' + "</td> </tr>";
				Str += "</table>";
				TrainDriving.innerHTML = Str;
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

// Make the DIV element draggable:
var gap = 20;
var active = false;
var collision = false;
var dragging = false;
var pos1=0, pos2=0, pos3=0, pos4=0;
var tdDrag = document.getElementById("traindrivingdiv");
	tdDrag.ontouchstart = dragMouseElement(document.getElementById("traindrivingdiv"));
	tdDrag.onclick = dragMouseElement(document.getElementById("traindrivingdiv"));

function dragMouseElement(tdDrag) {
	var offsetX = 0, offsetY = 0, initX = 0, initY = 0;
	tdDrag.ontouchstart = touchStart;
	tdDrag.onmousedown = initDrag;

	function touchStart(event) {
		event.preventDefault();
		var touch = event.touches[0];
		initX = touch.clientX;
		initY = touch.clientY;
		document.ontouchend = closeDrag;
		document.ontouchmove = touchMove;
	}

	function initDrag(event) {
		event.preventDefault();
		initX = event.clientX;
		initY = event.clientY;
		document.onmouseup = closeDrag;
		document.onmousemove = moveDrag;
	}

	function touchMove(event){
		event.preventDefault();
		dragging = true;
		var touch = event.touches[0];
		var tm = document.getElementById("TrackMonitor").getBoundingClientRect();
		var td = document.getElementById("TrainDriving").getBoundingClientRect();
		collision = isCollide(tm, td);
		if (collision){
			tdDrag.style.border = "2px solid gray";
			tdDrag.style.borderRadius = "24px";
		}else{
			tdDrag.style.border = "0px solid gray";
		}
		offsetX = initX - touch.clientX;
		offsetY = initY - touch.clientY;
		initX = touch.clientX;
		initY = touch.clientY;
		// avoids to overlap the trackmonitor div
		tdDrag.style.left = (collision && offsetX > 0 ? tdDrag.offsetLeft : tdDrag.offsetLeft - offsetX) + "px";// X
		tdDrag.style.top = (collision && offsetY > 0 ? tdDrag.offsetTop : tdDrag.offsetTop - offsetY) + "px";   // Y
		dragging = false;
	}

	function moveDrag(event) {
		event.preventDefault();
		dragging = true;
		var tm = document.getElementById("TrackMonitor").getBoundingClientRect();
		var td = document.getElementById("TrainDriving").getBoundingClientRect();
		collision = isCollide(tm, td);
		if (collision){ // detect collision
			tdDrag.style.border = "2px solid gray";
			tdDrag.style.borderRadius = "24px";
		}else{
			tdDrag.style.border = "0px solid gray";
		}
	offsetX = initX - event.clientX;
	offsetY = initY - event.clientY;
	initX = event.clientX;
		initY = event.clientY;
		// avoids to overlap the trackmonitor window
		tdDrag.style.left = (collision && offsetX > 0 ? tdDrag.offsetLeft : tdDrag.offsetLeft - offsetX) + "px";// X
		tdDrag.style.top = (collision && offsetY > 0 ? tdDrag.offsetTop : tdDrag.offsetTop - offsetY) + "px";   // Y
		dragging = false;
	}

	function closeDrag(event) {
		if(event.type === "touchcancel" || event.type === "touchend" ){
			if (dragging){
				return;
			}
			document.ontouchstart = null;
			document.ontouchmove = null;
		}else{
			if (dragging){
				return;
			}
			document.onmouseup = null;
			document.onmousemove = null;
		}
	}
}

function isCollide(a, b) {
	return !(
		((a.y + a.height + gap) < (b.y)) ||
		(a.y + gap > (b.y + b.height)) ||
		((a.x + a.width + gap) < b.x) ||
		(a.x + gap > (b.x + b.width))
	);
}
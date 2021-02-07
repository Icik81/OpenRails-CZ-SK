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
	if (initialIdleMs != null)
		idleMs = initialIdleMs; // Save it to use at end

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
					endIndexLast = 0,
					endIndexKey = 0;
					
				var keyPressedColor = "",
					newDataFirst = "",
					newDataLast = "",
					smallSymbolColor = "",
					stringColorFirst = "",
					stringColorLast = "";
				// Color codes
				var codeColor = ['???','??!','?!?','?!!','!??','!!?','!!!','%%%','$$$'];
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
						endIndexLast = obj.trainDrivingData[row].LastCol.length;
						newDataLast = obj.trainDrivingData[row].LastCol.slice(0, endIndexLast -3);
						stringColorLast = obj.trainDrivingData[row].LastCol.slice(-3);
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
					if (codeColor.indexOf(stringColorLast) != -1) { lastColor = true; }
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
							Str += "<td ColorCode=" + stringColorLast + ">" + newDataLast + "</td>";
						}
						else{
							Str += "<td>" + obj.trainDrivingData[row].LastCol + "</td>";
						}
					}
					Str += "</tr>";
				}
				Str += "</table>";
				// space at bottom
				Str += "<tr> <td colspan='5' style='text-align: center'>" + '.' + "</td> </tr>";
				Str += "</table>";
				TrainDriving.innerHTML = Str;
			}
		}
	}
}

function changePageColor() {
	var buttonClicked = document.getElementById("buttonDN");
	var bodyColor = document.getElementById("body");
	
	if (buttonClicked.innerHTML == "Day"){
		buttonClicked.innerHTML = "Night";
		bodyColor.style.background = "black";
		bodyColor.style.color =	"white";
	}
	else if (buttonClicked.innerHTML == "Night"){
		buttonClicked.innerHTML = "Day"
		bodyColor.style.background = "white";
		bodyColor.style.color =	"black";
	}
};
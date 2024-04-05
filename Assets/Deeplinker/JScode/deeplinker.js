var endPath = window.location.search;
var finalLink = "vigamegridlock://"
const deviceOS = getOS();
var hasParameters = window.location.search.includes("code");
console.log("valid content: " + hasParameters);
//code validity
if (hasParameters) {
    switch (deviceOS) {
        case 'ios':
            finalLink = "vigamegridlock://authentication/" + window.location.search;
            window.open(finalLink, "_blank");
            break;
        case 'Android':
            finalLink = "vigamegridlock://authentication/" + window.location.search;
            window.open(finalLink, "_blank");
        default:
            break;

    }
}

//Get device type
function getOS() {
    const userAgent = window.navigator.userAgent;
    if (userAgent.match(/iPad/i) || userAgent.match(/iPhone/i) || userAgent.match(/iPod/i)) {
        return 'iOS';
    }
    else if (userAgent.match(/Android/i)) {
        return 'Android';
    }
}
//Web UI code
window.onload = function () {
    if (!hasParameters) {
        document.getElementById("titleText").textContent = "Something has gone wrong";
        document.getElementById("subtitleText").textContent = "[Insert Error Message Here]";
    }
    else {
        document.getElementById("titleText").textContent = "Login Successful";
        document.getElementById("subtitleText").textContent = "you should be redirected to the app";
    }
};
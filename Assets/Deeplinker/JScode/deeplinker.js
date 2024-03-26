var endPath = window.location.search;
var finalLink = "vigamegridlock://"
const deviceOS = getOS();

switch (deviceOS) {
    case 'ios':
        finalLink = "vigamegridlock://" + window.location.search;
        break;
    case 'Android':
        finalLink = "vigamegridlock://" + window.location.search;
    default:
        break;
}
location.href = finalLink;

function getOS()
{
    const userAgent = window.navigator.userAgent;
    if( userAgent.match( /iPad/i ) || userAgent.match( /iPhone/i ) || userAgent.match( /iPod/i ) )
    {
       return 'iOS';
    }
    else if( userAgent.match( /Android/i ) )
    {
       return 'Android';
    }
}
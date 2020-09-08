
const REQUEST_SCENES_PAYLOAD = "RequestScenes";
const SCENES_CHANGED_PAYLOAD = "ScenesChanged";
const DEVICE_CHANGED_PAYLOAD = "DeviceChanged";
const SONGS_CHANGED_PAYLOAD = "SongsChanged";
const DEVICE_TOPIC = "/Theater/Device"
const SCENE_CONFIGURATION_TOPIC = "/Theater/Scene";
const SCENE_CONTROL_TOPIC = "/Theater/Control";
const SONG_TOPIC = "/Theater/songs";
const SELECTION_TOPIC = "/Theater/Selection";
const SONG_CONTROL_TOPIC_FROM_UI = "/Theater/SongControlUI";

const client  = mqtt.connect('ws://localhost:9001');
client.subscribe(SCENE_CONFIGURATION_TOPIC);
client.subscribe(DEVICE_TOPIC);
client.subscribe(SONG_TOPIC);
client.subscribe(SELECTION_TOPIC);

let queue = [];
let scenes = [];
let devices = [];
let songs = [];

let globalScenesList = [];

let selectedSceneIndex = null;
let selectedSongIndex = null;
let runningSceneIndex = 0;


let selectedSceneId = null;
let selectedDeviceId = null;
let selectedSongId = null;

client.on('message', function (topic, message) {
  queue.push(topic.toString(), message.toString())
  // client.end()
});
client.on('connect', () => {
    console.log('connected');
    client.publish(SCENE_CONFIGURATION_TOPIC,REQUEST_SCENES_PAYLOAD)
});
setInterval((topic, message) => {
    if(queue.length > 0){
        handleMessage(queue.shift(), queue.shift())
    }
}, 200);

/**
	 * Handles messages of subsribed topics
	 *
     * @param {string} topic - The topic the message was received on
	 * @param {string} message - The incoming message
	 * @return {void}
	 */ 
async function handleMessage(topic, fullMessage) {
    if(fullMessage === "" || fullMessage === REQUEST_SCENES_PAYLOAD){
        return
    }
    let message = fullMessage.split(";;;");

    switch (topic) {
        case SCENE_CONFIGURATION_TOPIC:
            scenes = message;
            let sceneList = document.getElementById('scenes');
            sceneList.innerHTML = "";
            const list = makeUL(scenes, "s", selectedSceneId);
            sceneList.appendChild(list);
            break;
        case SONG_TOPIC:
            songs = message;
            let songList = document.getElementById('songs');
            songList.innerHTML = "";
            songList.appendChild(makeUL(songs, "m", selectedSongId));
            break;
        case SELECTION_TOPIC:
            const splitPayload = message[0].split(' ');
            if (splitPayload[0] === "Scene") {
                if(selectedSceneId === null){
                    break;
                }
                const index = parseInt(selectedSceneId.toString().substring(2));
                if(index !== parseInt(splitPayload[1])){
                    changeIconVisibility(splitPayload[1]);
                }
            } else if (splitPayload[0] === "Song") {
                //selectedSongIndex = songs.indexOf(splitPayload[1])
            }
            break;
        default:
            devices = message;
            let deviceList = document.getElementById('devices');
            deviceList.innerHTML = "";
            deviceList.appendChild(makeUL(devices, "d", selectedDeviceId));
            break;
    }
}
createScenePlayer(selectedSceneId);
createMusicPlayer();

// ID-Template: prefix(d,s,m) + element(l,i,t) + index
function makeUL(array, idPrefix, selectionSafe) {
    selectedSceneId = idPrefix + "i" + 0;
    // Create the list element:
    let list = createElementWithClass("ul", "mdc-list listClass borderClass varElementClass");

    for (let i = 0; i < array.length; i++) {
        // Create the list item:
        let item = createElementWithClass("li", "mdc-list-item");
        item.tabIndex = "1";
        item.id = idPrefix + "l" + i;
        let icon = document.createElement("i");
        icon.className = "mdc-icon-button material-icons";
        icon.textContent = "favorite";
        icon.id = idPrefix + "i" + i;

        icon.style.visibility = "hidden";
        item.appendChild(icon);

        // Set its contents:
        let span = createElementWithClass("span", "mdc-list-item__text");
        let span2 =createElementWithClass("span", "mdc-list-item__ripple");
        let text = document.createTextNode(array[i]);
        //text.id = idPrefix + "t" + i;

        item.onfocus = () => {
            //if(selectedSceneId !== null){
            //    let lastSelection = document.getElementById(selectedSceneId);
            //    lastSelection.style.visibility = "hidden";
            //}
            //let currentSelection = document.getElementById(icon.id);
            //currentSelection.style.visibility = "visible";
            //selectionSafe = icon.id;
            const index = parseInt(icon.id.toString().substring(2));
            changeIconVisibility(index);
        };

        span.appendChild(text);
        span.appendChild(icon);
        item.appendChild(span2);
        item.appendChild(span);

        // Add it to the list:
        list.appendChild(item);

        if (i === array.length - 1){
            break;
        }
        let divider = createElementWithClass("li", "mdc-list-divider");
        divider.role = "separator";
        list.append(divider)
    }
    return list;
}

function createElementWithClass(elementName, className){
    let element = document.createElement(elementName);
    element.className = className;
    return element;
}

function createScenePlayer(sceneId){
    let scenePlayerBack = document.getElementById("scenePlayerBack");
    let scenePlayerPlay = document.getElementById("scenePlayerPlay");
    let scenePlayerNext = document.getElementById("scenePlayerNext");
    scenePlayerBack.onclick = () => {
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Previous");
        //const currentlySelectedSceneId = parseInt(selectedSceneId.toString().substring(2));
        //changeIconVisibility(currentlySelectedSceneId - 1)
    };
    scenePlayerPlay.onclick = () => {
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Play");
        document.getElementById(selectedSceneId).focus();
        //toggleIconAndPublish(scenePlayerPlay);
    };
    scenePlayerNext.onclick = () => {
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Next");
        //const currentlySelectedSceneId = parseInt(selectedSceneId.toString().substring(2));
        //changeIconVisibility(currentlySelectedSceneId + 1)
    };
}
function changeIconVisibility(newIndex){
    const i = parseInt(selectedSceneId.toString().substring(2));
    if(newIndex < 0 || newIndex > scenes.length -1) {
        return;
    }
    let id = parseInt(selectedSceneId.toString().substring(2));
    document.getElementById("s" + "i" + id).style.visibility = "hidden";
    document.getElementById("s" + "i" + newIndex).style.visibility = "visible";
    selectedSceneId = "s" + "i" + newIndex;
    sendMessageToServer("/Theater/Selection", "Scene " + newIndex);
}

function createMusicPlayer(){
    let musicPlayerBack = document.getElementById("musicPlayerBack");
    let musicPlayerShortBack = document.getElementById("musicPlayerShortBack");
    let musicPlayerPlay = document.getElementById("musicPlayerPlay");
    let musicPlayerShortNext = document.getElementById("musicPlayerShortNext");
    let musicPlayerNext = document.getElementById("musicPlayerNext");
    musicPlayerBack.onclick = () => sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "PrevSong");
    musicPlayerShortBack.onclick = () => sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "RunBack");
    musicPlayerPlay.onclick = () => {
        sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "Pause");
        toggleIcon(musicPlayerPlay, "play_arrow", "stop");
    };
    musicPlayerShortNext.onclick = () => sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "RunForward");
    musicPlayerNext.onclick = () => sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "NextSong");
}

function sendMessageToServer(topic, payload){
    client.publish(topic,payload);
}
function toggleIcon(player, icon1, icon2){
    if(player.textContent === icon1){
        player.textContent = icon2;
    }
    else{
        player.textContent = icon1
    }
}
function toggleIconAndPublish(player, icon1, icon2){
    if(player.textContent === icon1){
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Play");
    }
    else{
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Stop");
    }
    toggleIcon(player, icon1, icon2);
}
let resizeHandle = document.getElementById("sliderLeft");
let sceneHandle = document.getElementById("sceneHandle");
let deviceHandle = document.getElementById("deviceHandle");

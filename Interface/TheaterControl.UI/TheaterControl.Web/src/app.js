
const REQUEST_SCENES_PAYLOAD = "RequestScenes";
const SCENES_CHANGED_PAYLOAD = "ScenesChanged";
const DEVICE_CHANGED_PAYLOAD = "DeviceChanged";
const SONGS_CHANGED_PAYLOAD = "SongsChanged";
const SCENE_CONFIGURATION_TOPIC = "/Theater/Scene";
const SCENE_CONTROL_TOPIC = "/Theater/Control";
const SONG_TOPIC = "/Theater/songs";
const SELECTION_TOPIC = "/Theater/Selection";
SONG_CONTROL_TOPIC_FROM_UI = "/Theater/SongControlUI";

const client  = mqtt.connect('ws://linvm2416:9001');
client.subscribe('/Theater/Scene');
client.subscribe('/Theater/Device');
client.subscribe('/Theater/songs');

let queue = [];
let scenes = [];
let devices = [];
let songs = [];

let globalScenesList = [];

let selectedSceneIndex = null;
let selectedSongIndex = null;

client.on('message', function (topic, message) {
  queue.push(topic.toString(), message.toString())
  // client.end()
});

setInterval((topic, message) => {
    if(queue.length > 0){
        handleMessage(queue.shift(), queue.shift())
    }
}, 200);
client.on('connect', () => {
    console.log('connected');
    client.publish(SCENE_CONFIGURATION_TOPIC,REQUEST_SCENES_PAYLOAD)
});
createScenePlayer();
createMusicPlayer();
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
            const list = makeUL(scenes, "scene");
            sceneList.appendChild(list);
            globalScenesList = list;
            break;
        case SONG_TOPIC:
            songs = message;
            let songList = document.getElementById('songs');
            songList.innerHTML = "";
            songList.appendChild(makeUL(songs, "song"));
            break;
        case SELECTION_TOPIC:
            const splitPayload = message.split(' ');
            if (splitPayload[0] === "Scene") {
                selectedSceneIndex = int.Parse(splitPayload[1]) - 1;
            } else if (splitPayload[0] === "Song") {
                selectedSongIndex = songs.indexOf(splitPayload[1])
            }
            break;
        default:
            devices = message;
            let deviceList = document.getElementById('devices');
            deviceList.innerHTML = "";
            deviceList.appendChild(makeUL(devices, "device"));
            break;
    }
}
let selectedListItemId = null;
function makeUL(array, idPrefix) {
    // Create the list element:
    let list = createElementWithClass("ul", "mdc-list listClass borderClass varElementClass");
    for (let i = 0; i < array.length; i++) {
        // Create the list item:
        let item = createElementWithClass("li", "mdc-list-item");
        item.tabIndex = "1";
        item.id = idPrefix + "item" + i;
        let icon = document.createElement("i");
        icon.className = "mdc-icon-button material-icons";
        icon.textContent = "favorite";
        icon.id = idPrefix + "icon" + i.toString();
        icon.style.visibility = "hidden";
        item.appendChild(icon);

        item.onfocus = () => {
            if(selectedListItemId !== null){
                let lastSelection = document.getElementById(selectedListItemId);
                lastSelection.style.visibility = "hidden";
            }
            let currentSelection = document.getElementById(icon.id);
            currentSelection.style.visibility = "visible";
            selectedListItemId = icon.id;
        };
        // Set its contents:
        let span = createElementWithClass("span", "mdc-list-item__text");
        let span2 =createElementWithClass("span", "mdc-list-item__ripple");
        let text = document.createTextNode(array[i]);
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
function createScenePlayer(){
    let scenePlayerBack = document.getElementById("scenePlayerBack");
    let scenePlayerPlay = document.getElementById("scenePlayerPlay");
    let scenePlayerNext = document.getElementById("scenePlayerNext");
    scenePlayerBack.onclick = () => sendMessageToServer(SCENE_CONTROL_TOPIC, "Previous");
    scenePlayerPlay.onclick = () => toggleIconAndPublish(scenePlayerPlay);
    scenePlayerNext.onclick = () => sendMessageToServer(SCENE_CONTROL_TOPIC, "Next");
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

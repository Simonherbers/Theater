import {MDCRipple} from '@material/ripple/component'
import {MDCSlider} from '@material/slider/component'
const ripple = new MDCRipple(document.querySelector('.mdc-button'));
console.log("a")
ripple.layout()
ripple.activate()
ripple.activate()
ripple.activate()
ripple.activate()
ripple.activate()
ripple.activate()


const slider = new MDCSlider(document.querySelector('.mdc-slider'));
slider.listen('MDCSlider:change', () => console.log(`Value changed to ${slider.value}`));

const REQUEST_SCENES_PAYLOAD = "RequestScenes";
const SCENES_CHANGED_PAYLOAD = "ScenesChanged";
const DEVICE_CHANGED_PAYLOAD = "DeviceChanged";
const SONGS_CHANGED_PAYLOAD = "SongsChanged";
const DEVICE_TOPIC = "/Theater/Device";
const SCENE_CONFIGURATION_TOPIC = "/Theater/Scene";
const SCENE_CONTROL_TOPIC = "/Theater/Control";
const SONG_TOPIC = "/Theater/songs";
const SELECTION_TOPIC = "/Theater/Selection";
const SONG_CONTROL_TOPIC_FROM_UI = "/Theater/SongControlUI";

const SCENE_PREFIX = "s";
const SONG_PREFIX = "m";
const DEVICE_PREFIX = "d";

const client  = mqtt.connect('ws://localhost:9001');
client.subscribe(SCENE_CONFIGURATION_TOPIC);
client.subscribe(DEVICE_TOPIC);
client.subscribe(SONG_TOPIC);
client.subscribe(SELECTION_TOPIC);

let queue = [];
let scenes = [];
let devices = [];
let songs = [];

let selectedSceneId = "si0";
let selectedDeviceId = "di0";
let selectedSongId = "mi0";

client.on('message', function (topic, message) {
    const value = {topic: topic.toString(), message: message.toString()};
    queue.push(value)
});
client.on('connect', () => {
    console.log('connected');
    client.publish(SCENE_CONFIGURATION_TOPIC,REQUEST_SCENES_PAYLOAD)
});
setInterval((topic, message) => {
    if(queue.length > 0){
        const value = queue.shift();
        handleMessage(value.topic, value.message)
    }
}, 200);

/**
 * Applies the incoming changes to the different lists
 *
 * @param {string} topic - The topic the message was received on
 * @param {string} fullMessage - The incoming message
 * @return {void}
 */
function handleMessage(topic, fullMessage) { //was async
    if(fullMessage === "" || fullMessage === REQUEST_SCENES_PAYLOAD){
        return;
    }
    let message = fullMessage.split(";;;");

    switch (topic) {
        case SCENE_CONFIGURATION_TOPIC:
            scenes = message;
            let sceneList = document.getElementById('scenes');
            sceneList.innerHTML = "";
            const list = makeUL(scenes, SCENE_PREFIX);
            sceneList.appendChild(list);
            changeIconVisibility(SCENE_PREFIX, 0, 0, scenes);
            break;
        case SONG_TOPIC:
            songs = message;
            let songList = document.getElementById('songs');
            songList.innerHTML = "";
            songList.appendChild(makeUL(songs, SONG_PREFIX));
            changeIconVisibility(SONG_PREFIX, 0, 0, songs);
            break;
        case DEVICE_TOPIC:
            devices = message;
            let deviceList = document.getElementById('devices');
            deviceList.innerHTML = "";
            //deviceList.appendChild(makeUL(devices, DEVICE_PREFIX));
            deviceList.appendChild(makeDeviceList(devices, 8));
            changeIconVisibility(DEVICE_PREFIX, 0, 0, devices);
            break;

        case SELECTION_TOPIC:
            const payload = message[0];
            if (payload[0] === SCENE_PREFIX) {
                changeListItemSelection(SCENE_PREFIX, scenes, payload.substring(1))
            } else if (payload[0] === SONG_PREFIX) {
                changeListItemSelection(SONG_PREFIX, songs, payload.substring(1))
            }
            break;
    }
}

function changeListItemSelection(prefix, array, value){
    const oldIndex = parseInt(getSelectedItemIdInList(prefix).toString().substring(2));
    if(oldIndex !== parseInt(value)) {
        const newId = changeIconVisibility(prefix, oldIndex, value, array);
        setSelectedItemIdInList(newId);
    }
}

createScenePlayer();
createMusicPlayer();

// ID-Template: prefix(d,s,m) + element(l,i,t) + index
function makeUL(array, idPrefix) {
    // Create the list element:
    let list = createElementWithClass("ul", "mdc-list listClass borderClass varElementClass");

    for (let i = 0; i < array.length; i++) {

        //List item
        let item = createElementWithClass("li", "mdc-list-item");
        item.tabIndex = "1";
        item.id = idPrefix + "l" + i;

        //Icon
        let icon = document.createElement("i");
        icon.className = "mdc-icon-button material-icons";
        icon.textContent = "favorite";
        icon.id = idPrefix + "i" + i;
        icon.style.visibility = "hidden";

        //Text
        let span = createElementWithClass("span", "mdc-list-item__text");
        let span2 =createElementWithClass("span", "mdc-list-item__ripple");
        let text = document.createTextNode(array[i]);
        span.appendChild(text);
        span.appendChild(icon);
        item.appendChild(span2);
        item.appendChild(span);

        //Events

        item.onfocus = () => sendMessageToServer("/Theater/Selection", idPrefix + i);

        // Add it to the list:
        list.appendChild(item);

        //Divider
        if (i === array.length - 1){
            break;
        }
        let divider = createElementWithClass("li", "mdc-list-divider");
        divider.role = "separator";
        list.append(divider)
    }
    return list;
}

function makeDeviceList(array, rowLength) {
    let container = createElementWithClass("div", "div-container")
    let count = 0;
    for (let i = 0; i < 2; i++) {
        let row = createElementWithClass("div", "flex")
        //Use 8 cells for each row
        for (let j = 0; j < rowLength; j++) {
            let cell = createElementWithClass("div", "deviceSpanClass")
            if(array[count] !== undefined){
                const text = document.createTextNode(array[count]);
                cell.appendChild(text);
                cell.appendChild(createSlider());
            }
            count++;
            row.appendChild(cell)

            if(count === rowLength - 1)
            {
                break;
            }
            let span = createElementWithClass("span", "")
            let divider = createElementWithClass("div", "vl");
            span.appendChild(divider)
            row.appendChild(span)
        }
        container.appendChild(row);
        //container.appendChild(createElementWithClass("hr", ""))
    }
    return container;
}

function createSlider(){
    let slider = createElementWithClass("div", "mdc-slider mdc-slider--discrete");
    slider.role = "slider";
    slider.tabIndex = "0";
    slider.setAttribute("aria-label", "Intensity");
    slider.setAttribute("aria-valuemin", "0");
    slider.setAttribute("aria-valuemax", "255");
    slider.setAttribute("aria-valuenow", "0");

    let trackContainer = createElementWithClass("div", "mdc-slider__track-container");

    let thumbContainer = createElementWithClass("div", "mdc-slider__thumb-container");

    let pin = createElementWithClass("div", "mdc-slider__pin")

    let thumb = document.createElementNS("http://www.w3.org/2000/svg", "svg")
    thumb.setAttribute("class", "mdc-slider__thumb")
    thumb.setAttribute("width", "21");
    thumb.setAttribute("height", "21");

    let circle = document.createElementNS("http://www.w3.org/2000/svg", "circle")
    circle.setAttribute("cx", "10.5");
    circle.setAttribute("cy", "10.5");
    circle.setAttribute("r", "7.875");


    let focusRing = createElementWithClass("div", "mdc-slider__focus-ring");

    trackContainer.appendChild(createElementWithClass("div", "mdc-slider__track"));
    pin.appendChild(createElementWithClass("span", "mdc-slider__pin-value-marker"))
    thumbContainer.appendChild(pin)
    thumb.appendChild(circle);
    thumbContainer.appendChild(thumb);
    thumbContainer.appendChild(focusRing);

    slider.appendChild(trackContainer);
    slider.appendChild(thumbContainer);
    return slider;
}
function setSelectedItemIdInList(id){
    const prefix = id.toString()[0];
    switch(prefix){
        case SCENE_PREFIX:
            selectedSceneId = id;
            break;
        case SONG_PREFIX:
            selectedSongId = id;
            break;
        case DEVICE_PREFIX:
            selectedDeviceId = id;
            break;
    }
}
function getSelectedItemIdInList(prefix){
    switch(prefix){
        case SCENE_PREFIX:
            return selectedSceneId;
        case SONG_PREFIX:
            return selectedSongId;
        case DEVICE_PREFIX:
            return selectedDeviceId;
    }
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
    scenePlayerBack.onclick = () => {
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Previous");
    };
    scenePlayerPlay.onclick = () => {
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Play");
    };
    scenePlayerNext.onclick = () => {
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Next");
    };
}
/**
 * Hides the icon of the previously selected item, then turns the icon of the selected item visible
 *
 * @param {string} prefix - Array identifier
 * @param {int} oldIndex - Index of previous selection
 * @param {int} newIndex - Index of selection to show
 * @param {[]} array - The list to which the selection will be applied
 * @return {string}
 */
function changeIconVisibility(prefix, oldIndex, newIndex, array){
    if(newIndex < 0 || newIndex > array.length - 1) {
        return prefix + "i" + oldIndex.toString();
    }
    const id = prefix + "i" + oldIndex.toString();
    let el = document.getElementById(id);
    el.style.visibility = "hidden";
    document.getElementById(prefix + "i" + newIndex.toString()).style.visibility = "visible";
    return prefix + "i" + newIndex.toString();
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

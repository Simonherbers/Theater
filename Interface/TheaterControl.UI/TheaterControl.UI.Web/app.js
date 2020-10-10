import {MDCRipple} from '@material/ripple/component'
import {MDCTextField} from '@material/textfield';
import {MDCDialog} from '@material/dialog';
import {MDCSwitch} from '@material/switch';
const dialog = new MDCDialog(document.querySelector('.mdc-dialog'));

const textFields = [].map.call(document.querySelectorAll('.mdc-text-field'), function(el) {
    return new MDCTextField(el);
});

const buttons = [].map.call(document.querySelectorAll('.mdc-icon-button'), function(el) {
    return new MDCRipple(el);
});
const fabButtons = [].map.call(document.querySelectorAll('.mdc-fab'), function(el) {
    return new MDCRipple(el);
});
const switchControl = new MDCSwitch(document.querySelector('.mdc-switch'));

const REQUEST_SCENES_PAYLOAD = "RequestScenes";
const DEVICE_TOPIC = "/Theater/Device";
const SCENE_CONFIGURATION_TOPIC = "/Theater/Scene";
const SCENE_CONTROL_TOPIC = "/Theater/Control";
const SONG_TOPIC = "/Theater/songs";
const SELECTION_TOPIC = "/Theater/Selection";
const SONG_CONTROL_TOPIC_FROM_UI = "/Theater/SongControlUI";

const SCENE_PREFIX = "s";
const SONG_PREFIX = "m";
const DEVICE_PREFIX = "d";

const DEVICE_LIST_ROW_LENGTH = 8;
const DEVICE_LIST_CONTROL_ONCLICK_VALUE_CHANGE = 10;

const client  = mqtt.connect('ws://localhost:9001', {clean : true});

let queue = [];
let scenes = [];
let devices = [];
let songs = [];

let selectedSceneId = "si0";
let selectedSongId = "mi0";

let currentTheme = 0;

let configurationButton = document.getElementById("configuration");
configurationButton.onclick = () => dialog.open();
let radio1 = document.getElementById("radio-1");
let radio2 = document.getElementById("radio-2");
radio1.onclick = () => changeLayout(1);
radio2.onclick = () => changeLayout(2);

client.on('message', function (topic, message) {
    const value = {topic: topic.toString(), message: message.toString()};
    queue.push(value)
});
client.on('connect', () => {
    client.subscribe(SCENE_CONFIGURATION_TOPIC);
    client.subscribe(DEVICE_TOPIC);
    client.subscribe(SONG_TOPIC);
    client.subscribe(SELECTION_TOPIC);
    console.log('connected');
    sendMessageToServer(SCENE_CONFIGURATION_TOPIC,REQUEST_SCENES_PAYLOAD)
});
client.on('disconnect', () => console.log("disconnected"));
client.on('reconnect', () => console.log("reconnected"));

setInterval((topic, message) => {
    if(queue.length > 0){
        const value = queue.shift();
        handleMessage(value.topic, value.message)
    }
}, 50);

export function changeTheme(){
    const myNode = document.querySelector('.bodyClass');
    const isDarkTheme = myNode.classList.contains('darkTheme');
    const removeDarkBackground = () => myNode.classList.remove('darkTheme');
    const addDarkBackground = () => myNode.classList.add('darkTheme');
    if (isDarkTheme) removeDarkBackground();
    else addDarkBackground();
    return true;
}

/**
 * Applies the incoming changes to the different lists
 *
 * @param {string} topic - The topic the message was received on
 * @param {string} fullMessage - The incoming message
 * @return {void}
 */
function handleMessage(topic, fullMessage) {
    if(fullMessage === "" || fullMessage === REQUEST_SCENES_PAYLOAD){
        return;
    }
    let message = fullMessage.split(";;;");

    switch (topic) {
        case SCENE_CONFIGURATION_TOPIC:
            scenes = message;
            let sceneList = document.getElementById('scenes');
            sceneList.innerHTML = "";
            const list = createItemList(scenes, SCENE_PREFIX);
            sceneList.appendChild(list);
            changeIconVisibility(SCENE_PREFIX, 0, 0, scenes);
            selectedSceneId = "si0";
            break;
        case SONG_TOPIC:
            songs = message;
            let songList = document.getElementById('songs');
            songList.innerHTML = "";
            songList.appendChild(createItemList(songs, SONG_PREFIX));
            changeIconVisibility(SONG_PREFIX, 0, 0, songs);
            selectedSongId = "mi0";
            break;
        case DEVICE_TOPIC:
            devices = message;
            subscribeToDeviceTopics(devices);
            let deviceList = document.getElementById("devices");
            deviceList.innerHTML = "";
            deviceList.appendChild(createItemList(devices, DEVICE_PREFIX));
            break;

        case SELECTION_TOPIC:
            const payload = message[0];
            if (payload[0] === SCENE_PREFIX) {
                changeListItemSelection(SCENE_PREFIX, scenes, payload.substring(1))
            } else if (payload[0] === SONG_PREFIX) {
                changeListItemSelection(SONG_PREFIX, songs, payload.substring(1))
            }
            break;

        default:
            const device = topic.substring(9); // base_topic = /Theater/
            const res = devices.find(x => x === device);
            if(res !== undefined){
                changeElementValue(res, message[0]);
            }

    }
}
function subscribeToDeviceTopics(devices){
    for (let i = 0; i < devices.length; i++) {
        const topic = "/Theater/" + devices[i];
        client.subscribe(topic);
    }
}

function changeLayout(selection){
    switch (selection) {
        case 1:
            reOrderElements("scenes", "songs", "devices");
            break;
        case 2:
            reOrderElements("devices", "scenes", "songs");
            break;
    }
}
function changeListItemSelection(prefix, array, value){
    console.log(value);
    const oldIndex = parseInt(getSelectedItemIdInList(prefix).toString().substring(2));
    if(oldIndex !== parseInt(value)) {
        const newId = changeIconVisibility(prefix, oldIndex, value, array);
        setSelectedItemIdInList(newId);
    }
}

// ID-Template: prefix(d,s,m) + element(l,i,t) + index
function makeUL(array, idPrefix) {
    // Create the list element:
    let list = createElementWithClass("ul", "mdc-list listClass");
    list.id = idPrefix === "s" ? "scenes" : idPrefix === "m" ? "songs" : "devices";

    for (let i = 0; i < array.length; i++) {

        //List item
        let item = createElementWithClass("li", "mdc-list-item");
        item.tabIndex = "0";
        item.id = idPrefix + "l" + i;

        //Icon
        let icon = createElementWithClass("i", "mdc-icon-button material-icons mdc-theme--secondary");
        icon.textContent = "favorite";
        icon.id = idPrefix + "i" + i;
        icon.style.visibility = "hidden";

        //Text
        let span = createElementWithClass("span", "mdc-list-item__text");
        let ripple =createElementWithClass("span", "mdc-list-item__ripple");
        let text = document.createTextNode(array[i]);
        span.appendChild(text);
        span.appendChild(icon);
        item.appendChild(ripple);
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
    list = encapsulateElementInNewElement(list,"div", "mdc-card--outlined cardClass container innerCard");//////
    const h = screen.height * 0.7;
    const w = screen.width * 0.44;
    list.style.width = w + "px";
    list.style.height = h + "px";
    list.style.maxWidth = w + "px";
    list.style.maxHeight = h + "px";
    return list;
}

function reOrderElements(first, second, third){
    let container = document.getElementById("overallContainer");
    container.className = "overallContainers";

    let list = [];
    list[0] =  document.getElementById(first).cloneNode(true);
    list[1] =  document.getElementById(second).cloneNode(true);
    list[2] =  document.getElementById(third).cloneNode(true);

    container.innerHTML = "";
    for(let i = 0; i < 3; i++){
        const elementInDiv = encapsulateElementInNewElement(list[i], "div", "");
        container.appendChild(elementInDiv);
    }
}

function createItemList(array, prefix){
    let card = createElementWithClass("div", "mdc-card--outlined cardClass outerCard");
    let list = prefix === DEVICE_PREFIX ? makeDeviceList(array) : makeUL(array, prefix);
    let actions = prefix === "s" ? createScenePlayer() : prefix === SONG_PREFIX ? createMusicPlayer() : document.createElement("div");
    const text = prefix === "s" ? "Scenes" : prefix === SONG_PREFIX ? "Songs" : "Devices";
    let title = encapsulateElementInNewElement(document.createTextNode(text), "div", "mdc-typography--headline4 mdc-theme--primary listHeader");

    card.appendChild(title);
    card.appendChild(list);

    let player = createElementWithClass("div", "player");
    player.appendChild(actions);

    if(prefix === SCENE_PREFIX){
        let scenePlayerEmergency = createFabButton("stop");
        scenePlayerEmergency.onclick = () => sendMessageToServer(SCENE_CONTROL_TOPIC, "Emergency");
        player.appendChild(scenePlayerEmergency);
    }

    card.appendChild(player);

    card = encapsulateElementInNewElement(card, "div", "container");/////
    card.id = prefix + "Card";
    return card;
}


/**
 * Creates a table of devices with controls from an array.
 *
 * @param {string[]} array - The array containing all device names.
 * @return {any}
 */
function makeDeviceList(array) {
    let devicePanel = createElementWithClass("div", "sliderContainer");
    let master = createElementWithClass("div", "deviceBox container");

    let masterLabel = document.createTextNode("Master");
    masterLabel = encapsulateElementInNewElement(masterLabel, "h1", "mdc-typography--headline5");
    masterLabel = encapsulateElementInNewElement(masterLabel, "div", "container-title");

    master.appendChild(masterLabel);

    let inputContainer = createInputField("masterInput", "master");
    master.appendChild(inputContainer);

    let masterSlider = createDefaultSlider(0.3, inputContainer, "masterSlider");
    master.appendChild(masterSlider);
    devicePanel.appendChild(master);

    let divider = createElementWithClass("div", "verticalDivider")
    devicePanel.appendChild(divider);

    let container = createElementWithClass("div", "");
    let count = 0;

    const rows = Math.ceil(array.length / DEVICE_LIST_ROW_LENGTH);

    for (let i = 0; i < rows; i++) {

        let row = createElementWithClass("div", "containers");

        for (let j = 0; j < DEVICE_LIST_ROW_LENGTH; j++) {

            let container = createElementWithClass("div", "deviceBox container");
            const width = Math.floor(100 / DEVICE_LIST_ROW_LENGTH);
            container.style.width = width + "%";
            if(array[count] !== undefined){

                //device name
                let deviceName = array[count]
                let text = document.createTextNode(deviceName);
                text = encapsulateElementInNewElement(text, "h1", "mdc-typography--headline5");
                text = encapsulateElementInNewElement(text, "div", "container-title");
                text.title = deviceName
                //outlined text field
                const index = j + i * DEVICE_LIST_ROW_LENGTH;
                let label = createInputField("my-label-id-" + index, deviceName);

                //control buttons
                container.appendChild(text);
                container.appendChild(label);
                container.appendChild(createDefaultSlider(0.1, label, array[index], deviceName));
            }
            count++;

            row.appendChild(container);

            if(count !== (DEVICE_LIST_ROW_LENGTH * (i + 1)))
            {
                let divider = createElementWithClass("div", "verticalDivider")
                row.appendChild(divider);
            }
        }
        container.appendChild(row);
        if(i < (rows - 1)){
            container.appendChild(createElementWithClass("hr", i));
        }
    }
    devicePanel.appendChild(container);
    devicePanel = encapsulateElementInNewElement(devicePanel, "div", "mdc-card--outlined cardClass");
    devicePanel.id = "deviceCard";

    return devicePanel;
}

function createInputField(id, name){
    let label = createElementWithClass("label", "mdc-text-field mdc-text-field--outlined");
    let inp = createElementWithClass("input", "mdc-text-field__input");
    inp.id = id;
    inp.value = 0;
    inp.name = name;
    if(name !== "master"){
        inp.onkeyup = () => sendMessageToServer("/Theater/" + name, inp.value);
    }
    let notchedOutline = createElementWithClass("span", "mdc-notched-outline");
    let outlineLead = createElementWithClass("span", "mdc-notched-outline__leading");
    let outlineNotch = createElementWithClass("span", "mdc-notched-outline__notch");
    let outlineTrailing = createElementWithClass("span", "mdc-notched-outline__trailing");
    label.appendChild(inp);
    notchedOutline.appendChild(outlineLead);
    notchedOutline.appendChild(outlineNotch);
    notchedOutline.appendChild(outlineTrailing);

    label.appendChild(notchedOutline);
    return label;
}

function changeElementValue(name, value){
    value = parseInt(value);
    let elements = document.getElementsByName(name);
    for(let i = 0; i < elements.length; i++)
    {
        elements[i].value = value > 100 ? 100 : value < 0 ? 0 : value;
    }
}
function encapsulateElementInNewElement(element, newElement, className){
    const div = createElementWithClass(newElement, className);
    div.appendChild(element);
    return div;
}


function createDefaultSlider(factor, input, id, deviceName){
    let slider = createElementWithClass("INPUT", "slider" );
    slider.setAttribute("type", "range");
    slider.orientation = "vertical";
    slider.min = 0;
    slider.max = 100;
    slider.name = deviceName;
    slider.setAttribute("value","0");
    let newid = "slider" + id;
    slider.setAttribute("id", newid);
    slider.onmouseup = () => {
        let sl = document.getElementById(newid);
        sendMessageToServer("/Theater/" + id, sl.value);
        input.value = sl.value;
    };

    slider = encapsulateElementInNewElement(slider, "div", "sliderContainer");
    return slider;
}

function createFabButton(iconName, iconReferenceId){
    let button = createElementWithClass("button", "mdc-fab fabButton");
    const div = createElementWithClass("div", "mdc-fab__ripple");
    let span = createElementWithClass("span", "mdc-fab__icons material-icons");
    span.innerHTML = iconName;
    span.id = iconReferenceId
    button.appendChild(div);
    button.appendChild(span);
    return button;
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
    }
}
function getSelectedItemIdInList(prefix){
    switch(prefix){
        case SCENE_PREFIX:
            return selectedSceneId;
        case SONG_PREFIX:
            return selectedSongId;
    }
}

function createElementWithClass(elementName, className){
    let element = document.createElement(elementName);
    element.className = className;
    return element;
}

function createScenePlayer(){
    let actions = createElementWithClass("sceneActions", "playerActionsClass");

    let scenePlayerBack = createFabButton("skip_previous");
    let scenePlayerPlay = createFabButton("play_arrow", );
    let scenePlayerNext = createFabButton("skip_next");


    scenePlayerBack.onclick = () => {
        let id = "s" + (parseInt(getSelectedItemIdInList("s")[2]) - 1).toString();
        sendMessageToServer("/Theater/Selection", id);
    };
    scenePlayerPlay.onclick = () => {
        sendMessageToServer(SCENE_CONTROL_TOPIC, "Play");
    };
    scenePlayerNext.onclick = () => {
        let id = "s" + (parseInt(getSelectedItemIdInList("s")[2]) + 1).toString();
        sendMessageToServer("/Theater/Selection", id);
    };


    actions.appendChild(scenePlayerBack);
    actions.appendChild(scenePlayerPlay);
    actions.appendChild(scenePlayerNext);
    actions = encapsulateElementInNewElement(actions, "div", "");

    actions.className = "playerActionsClass";

    return actions;
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
    let actions = createElementWithClass("musicActions", "playerActionsClass");

    let musicPlayerBack = createFabButton("skip_previous");
    let musicPlayerShortBack = createFabButton("replay_5");
    let musicPlayerPlay = createFabButton("play_arrow", "musicPlayerPlayPause");
    let musicPlayerShortNext = createFabButton("forward_5");
    let musicPlayerNext = createFabButton("skip_next");

    musicPlayerBack.onclick = () => {
        const selected = getSelectedItemIdInList("m");
        const index = selected[0] + (parseInt(selected[2]) - 1).toString();
        sendMessageToServer("/Theater/Selection", index);
    };
    musicPlayerShortBack.onclick = () => sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "RunBack");
    musicPlayerPlay.onclick = () => {
        sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "Pause");
        document.getElementById("musicPlayerPlayPause").innerHTML =
            document.getElementById("musicPlayerPlayPause").innerHTML === "pause"
                ? "play_arrow"
                : "pause";
    };
    musicPlayerShortNext.onclick = () => sendMessageToServer(SONG_CONTROL_TOPIC_FROM_UI, "RunForward");
    musicPlayerNext.onclick = () => {
        const selected = getSelectedItemIdInList("m");
        const index = selected[0] + (parseInt(selected[2]) + 1).toString();
        sendMessageToServer("/Theater/Selection", index);
    };

    actions.appendChild(musicPlayerBack);
    actions.appendChild(musicPlayerShortBack);
    actions.appendChild(musicPlayerPlay);
    actions.appendChild(musicPlayerShortNext);
    actions.appendChild(musicPlayerNext);

    return actions;
}

function sendMessageToServer(topic, payload){
    client.publish(topic,payload);
    console.log(payload)
}

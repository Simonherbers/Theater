"use strict";

const express = require("express")
const path = require("path")

const app = express();
console.log(path.join(__dirname, "static"))

app.use("/static", express.static(path.join(__dirname, "static")));

app.get("/", (_, response) => response.sendFile(path.join(__dirname, "index.html")));
app.get("*", (_, response) => response.sendFile(path.join(__dirname, "404.html")));

app.listen("8080", _ => console.log("Listing on port 8080")); 
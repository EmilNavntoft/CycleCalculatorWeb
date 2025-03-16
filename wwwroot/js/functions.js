import Module from "./coolprop.js"

export function PropsSI(prop0, prop1, val1, prop2, val2, fluid) {
    console.log(prop0);
    console.log(prop1);
    console.log(prop2);
    console.log(val1);
    console.log(val2);
    console.log(fluid);
    const value = Module.PropsSI(prop0, prop1, val1, prop2, val2, fluid);
    console.log(value);
    return value;
}

function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}

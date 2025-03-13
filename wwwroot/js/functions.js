import Module from "./coolprop.js"

export async function updatePH(fluid) {
    const density = Module.PropsSI('D', 'T', 298.15, 'P', 100e5, fluid);
    return density;
}

function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}

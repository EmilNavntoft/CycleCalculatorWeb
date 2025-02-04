function saveAsFile(filename, bytesBase64) {
    var link = document.createElement('a');
    link.download = filename;
    link.href = "data:application/octet-stream;base64," + bytesBase64;
    document.body.appendChild(link); // Needed for Firefox
    link.click();
    document.body.removeChild(link);
}

function updatePH() {
    const T = Module.PropsSI("T", "P", 100000, "T", 300, "R134a");
    const rho = Module.PropsSI("D", "P", 100000, "T", 300, "R134a");
    const p = Module.PropsSI("P", "P", 100000, "T", 300, "R134a");
    const s = Module.PropsSI("S", "P", 100000, "T", 300, "R134a");
    const h = Module.PropsSI("H", "P", 100000, "T", 300, "R134a");
    const cp = Module.PropsSI("C", "P", 100000, "T", 300, "R134a");

    console.log(h);
}
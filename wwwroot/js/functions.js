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

window.saveAsFile = async function (fileName, contentStreamReference) {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

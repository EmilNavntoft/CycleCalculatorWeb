let coolpropInstance = null;

window.initializeCoolprop = async function () {
    if (!coolpropInstance) {
        const Module = (await import("./coolprop.js")).default;
        coolpropInstance = await Module();
    }
}

export function PropsSI(prop0, prop1, val1, prop2, val2, fluid) {
    if (!coolpropInstance) {
        throw new Error("CoolProp not initialized. Call initialize() first.");
    }

    console.log(prop0, prop1, val1, prop2, val2, fluid);
    const value = coolpropInstance.PropsSI(prop0, prop1, val1, prop2, val2, fluid);
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

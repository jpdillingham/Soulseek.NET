export const formatSeconds = (seconds) =>
{
    if (isNaN(seconds)) return '';
    var date = new Date(1970,0,1);
    date.setSeconds(seconds);
    return date.toTimeString().replace(/.*(\d{2}:\d{2}).*/, "$1");
}

export const formatBytes = (bytes, decimals = 2) => {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
}

export const getFileName = (fullPath) => {
    return fullPath.split('\\').pop().split('/').pop();
}

export const getDirectoryName = (fullPath) => {
    let path = fullPath.substring(0, fullPath.lastIndexOf("/"));
    return path.substring(0, path.lastIndexOf("\\"));
}
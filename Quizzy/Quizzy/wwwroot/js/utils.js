const localStorageKeys = {
    UserId: "U_ID"
}

const GetFromLocalStorage = (key) => {
    if (!Object.values(localStorageKeys).includes(key)) throw new Error("Invalid local storage key");
    const item = localStorage.getItem(key);
    if (item) {
        return JSON.parse(item);
    }
    else {
        return null;
    }
}

const SetLocalStorage = (key, value) => {
    if (!Object.values(localStorageKeys).includes(key)) throw new Error("Invalid local storage key");
    localStorage.setItem(key, JSON.stringify(value));
}

export { localStorageKeys, GetFromLocalStorage, SetLocalStorage }
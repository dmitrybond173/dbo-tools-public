/*
 * OS Services List (node.js edition)
 *
 * Generic app routinues.
 *
 * Written by Dmitry Bond. (dmitry.bond.real@gmail.com) @ Aug-Sep 2024
 * (ported my older Services List web app written on Asp.Net)
*/

import { readFileSync, writeFileSync } from 'node:fs';

export const saveToFile = (filename, value) => {
  try {
    writeFileSync(filename, value);
    //console.log('Write operation complete (sync).');
  } catch (err) {
    console.error(`ERR: fail writing to file (${filename}): ${err}`);
  }
}

export const loadPackageInfo = () => {
  const data = readFileSync('./package.json');
  return JSON.parse(data);
}

export const jsonCensor = (censor) => {
  var i = 0;

  return function (key, value) {
    if (i !== 0 && typeof (censor) === 'object' && typeof (value) == 'object' && censor == value)
      return '[Circular]';

    if (i >= 29) // seems to be a harded maximum of 30 serialized objects?
      return '[Unknown]';

    ++i; // so we know we aren't using the original object anymore

    return value;
  }
}

JSON.stringifyWithCircularRefs = (function () {
  const refs = new Map();
  const parents = [];
  const path = ["this"];

  function clear() {
    refs.clear();
    parents.length = 0;
    path.length = 1;
  }

  function updateParents(key, value) {
    var idx = parents.length - 1;
    var prev = parents[idx];
    if (prev[key] === value || idx === 0) {
      path.push(key);
      parents.push(value);
    } else {
      while (idx-- >= 0) {
        prev = parents[idx];
        if (prev[key] === value) {
          idx += 2;
          parents.length = idx;
          path.length = idx;
          --idx;
          parents[idx] = value;
          path[idx] = key;
          break;
        }
      }
    }
  }

  function checkCircular(key, value) {
    if (value != null) {
      if (typeof value === "object") {
        if (key) { updateParents(key, value); }

        let other = refs.get(value);
        if (other) {
          return '[Circular Reference]' + other;
        } else {
          refs.set(value, path.join('.'));
        }
      }
    }
    return value;
  }

  return function stringifyWithCircularRefs(obj, space) {
    try {
      parents.push(obj);
      return JSON.stringify(obj, checkCircular, space);
    } finally {
      clear();
    }
  }
})();


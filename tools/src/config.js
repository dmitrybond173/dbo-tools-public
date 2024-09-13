/*
 * OS Services List (node.js edition)
 *
 * App configuration.
 *
 * Written by Dmitry Bond. (dmitry.bond.real@gmail.com) @ Aug-Sep 2024
 * (ported my older Services List web app written on Asp.Net)
*/

import cfg from 'config';

export const CONFIG = cfg.get('config');
console.log(CONFIG);
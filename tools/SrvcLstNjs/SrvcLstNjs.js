/*
 * OS Services List (node.js edition)
 *
 * Entry-point script.
 *
 * Written by Dmitry Bond. (dmitry.bond.real@gmail.com) @ Aug-Sep 2024
 * (ported my older Services List web app written on Asp.Net)
*/

import express from 'express';
import { CONFIG } from './src/config.js';
import { format } from "date-fns";
import * as os from 'os';
import { readFile } from 'fs';
import { ScManager } from './src/scManager.js';
import * as util from './src/utils.js';
import isElevated from 'is-elevated';

//import { create, findAll, findByLogin, remove, update } from './src/users/users.controller.js';

//console.log(`${process.env}`);

//util.saveToFile('c:/temp/process.json', JSON.stringifyWithCircularRefs(process));
//util.saveToFile('c:/temp/process-env.json', JSON.stringify(process.env));

let pkgInfo = util.loadPackageInfo();
//let buildInfoStr = `${process.env.npm_package_name}[${process.env.npm_package_version}] @ [${process.env.npm_package_json}]`;
let buildInfoStr = `${pkgInfo.name}[${pkgInfo.version}] @ [${pkgInfo.author}]`;
isElevated().then(value => {
    let mode = (value ? "admin" : "normal-user");
    buildInfoStr += ` app-mode: ${mode}`;
});

var scm = new ScManager();

const app = express();

app.set('view engine', 'ejs');

app.use(express.json());

app.use('/media', express.static('public'));
app.use('/modules', express.static('node_modules'));

app.use((req, res, next) => {
    console.log(`app.use: ${req.method} ${req.path} ${req.url} [${JSON.stringify(req.query)}]`);
    //util.saveToFile('c:/temp/req-query.txt', JSON.stringify(req.query));

    if (req.method === 'GET') {
        let handled = true;

        if (/favicon\.ico/.test(req.url)) {
            readFile("public/favicon.ico", function (err, data) {
                if (err) {
                    console.warn(err);
                } else {
                    console.log('+ favicon loaded');
                    res.setHeader("Content-Type", "image/x-icon");
                    res.end(data);
                }
            });
        }
        else if (req.path === "/api/services") {
            if (req.query.act == undefined) {
                scm.servicesList.then(
                    () => {
                        console.warn(`--- promis resolved. ${scm.services.length} items in list...`);

                        res.writeHead(200, { 'Content-Type': 'application/json' });
                        res.end(JSON.stringify(scm.services));
                    },
                    () => {
                        res.writeHead(500, { 'Content-Type': 'text/plain' });
                        res.end("Fail to load list of services!");
                    }
                );
            }
            else if (req.query.act === 'start' || req.query.act === 'stop') {
                let msg = ` +++ need to [${req.query.act}] service [${req.query.name}]`;
                console.log(msg);
                let action = req.query.act;
                let isStart = (action === 'start');
                let srvcName = req.query.name;
                let p = (isStart ? scm.startService(srvcName) : scm.stopService(srvcName));
                p.then(() => {
                    res.writeHead(200, { 'Content-Type': 'text/plain' });
                    res.end(msg);
                }).catch(() => {
                    res.writeHead(500, { 'Content-Type': 'text/plain' });
                    res.end(`Fail to ${action} service [${srvcName}]!`);
                });
            }
        }
        else
            handled = false;

        if (handled)
            return;
    }

    next();
});

app.get('/', (req, res) => {
    console.log(`app.get: ${req.method} ${req.path} ${req.url} [${JSON.stringify(req.query)}]`);
    console.log(JSON.stringify(req.query));

    let isShowAll = CONFIG.showAll;
    let chk = req.query["chkShowAll"];
    isShowAll = (chk != undefined && chk === 'on');

    scm.servicesList.then(
        () => {
            let list = (isShowAll ? scm.services : scm.services.filter(it => it.isControllable));
            res.render('pages/index', {
                buildInfo: buildInfoStr,
                hostname: os.hostname(),
                totalServicesCount: scm.services.length,
                servicesCount: list.length,
                services: list,
                loadedAt: format(scm.loadedAt, "yyyy-MM-dd,hh:mm:ss"),
                errorMsg: '',
                showAllServices: isShowAll,
            });
        },
        () => {
            res.writeHead(500, { 'Content-Type': 'text/plain' });
            res.end("Fail to load list of services!");
        }
    );
});

app.listen(CONFIG.port, () => {
    console.log('Server successfuly started on port ' + CONFIG.port);
});

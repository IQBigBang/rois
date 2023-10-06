const { workspace, languages, window, Diagnostic, Uri, Range, DiagnosticSeverity } = require('vscode');
//import { workspace, languages, window } from 'vscode';
const { access, constants } = require('node:fs');
//import { access, constants } from 'node:fs';
const { execFile } = require('node:child_process');
//import { execFile } from 'node:child_process';

/**
 * @type {import('vscode').DiagnosticCollection}
 */
let diagnosticCollection = null;

/**
 * @param {import('vscode').ExtensionContext} context 
 */
function activate(context) {
    // @ts-ignore
    console.log('Activated!');
    diagnosticCollection = languages.createDiagnosticCollection('rois');
    workspace.onDidSaveTextDocument(onSavedDocument);
    //let diagnostics = vscode.languages.createDiagnosticCollection('roisDefault');
}

/**
 * @param {import('vscode').TextDocument} doc 
 */
function onSavedDocument(doc) {
    if (doc.fileName.split('.')[doc.fileName.split('.').length-1] != "ro") return;
    const lspPath = workspace.getConfiguration('rois').get('lspPath');
    access(lspPath, constants.X_OK, (err) => {
        if (err) {
            window.showErrorMessage("Couldn't find RoisLSP path");
            return;
        }
        // execute it
        var filePath = doc.uri.fsPath;
        execFile(lspPath, [filePath, '-o', 'IRRELEVANT', '-v', '-d', '-json-errors'], 
            (error, stdout, stderr) => {
                if (error == null || error.code == 0) {
                    // no errors in code
                    diagnosticCollection.set(doc.uri, []);
                    return;
                }
                if (error.code != 107) {
                    window.showErrorMessage("RoisLSP executable failed")
                    return;
                }
                // parse stderr as json which looks like this:
                // {"type": "error", "where": "{line}:{column}", "message": "{message}"} 
                const errInfo = JSON.parse(stderr);
                const errLine = parseInt(errInfo.where.split(':')[0]);
                const errColumn = parseInt(errInfo.where.split(':')[1]);
                const errMessage = errInfo.message;
                // now show the diagnostic
                const range = new Range(errLine-1, errColumn-1, errLine-1, errColumn-1+3);
                const diagnostic = new Diagnostic(range,
                                    errMessage, DiagnosticSeverity.Error);
                callToSet(doc.uri, diagnostic);
            });
    });
    
}

function callToSet(fileUri, diag) {
    diagnosticCollection.set(fileUri, [diag]);
}

module.exports = { activate };
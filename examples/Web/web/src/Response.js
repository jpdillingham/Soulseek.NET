import React, { Component } from 'react';
import axios from 'axios';

import { baseUrl } from './config';
import { formatBytes, getDirectoryName, downloadFile, getFileName } from './util';

import FileList from './FileList'

import { 
    Button, 
    Card, 
    Icon,
    Label
} from 'semantic-ui-react';

const buildTree = (files) => {
    return files.reduce((dict, file) => {
        let dir = getDirectoryName(file.filename);
        let selectable = { selected: false, ...file };
        dict[dir] = dict[dir] === undefined ? [ selectable ] : dict[dir].concat(selectable);
        return dict;
    }, {});
}

class Response extends Component {
    state = { 
        tree: buildTree(this.props.response.files), 
        downloadRequest: undefined, 
        downloadError: '' 
    }

    onFileSelectionChange = (file, state) => {
        file.selected = state;
        this.setState({ tree: this.state.tree, downloadRequest: undefined, downloadError: '' })
    }

    download = (username, files, toBrowser = false) => {
        this.setState({ downloadRequest: 'inProgress' }, () => {
            Promise.all(files.map(f => this.downloadOne(username, f, toBrowser)))
            .then(() => this.setState({ downloadRequest: 'complete' }))
            .catch(err => this.setState({ downloadRequest: 'error', downloadError: err.response }))
        });
    }

    downloadOne = (username, file, toBrowser) => {
        if (toBrowser) {
            return axios.request({
                method: 'GET',
                url: `${baseUrl}/files/${username}/${encodeURI(file.filename)}`,
                responseType: 'arraybuffer',
                responseEncoding: 'binary'
            })
            .then((response) => { 
                if (toBrowser) { 
                    downloadFile(response.data, getFileName(file.filename))
                }
            });
        }

        return axios.post(`${baseUrl}/transfers/downloads/${username}/${encodeURI(file.filename)}`);
    }

    render = () => {
        let response = this.props.response;
        let free = response.freeUploadSlots > 0;

        let { tree, downloadRequest, downloadError } = this.state;

        let selectedFiles = Object.keys(tree)
            .reduce((list, dict) => list.concat(tree[dict]), [])
            .filter(f => f.selected);

        let selectedSize = formatBytes(selectedFiles.reduce((total, f) => total + f.size, 0));

        return (
            <Card className='result-card' raised>
                <Card.Content>
                    <Card.Header><Icon name='circle' color={free ? 'green' : 'yellow'}/>{response.username}</Card.Header>
                    <Card.Meta className='result-meta'>
                        <span>Upload Speed: {formatBytes(response.uploadSpeed)}/s, Free Upload Slot: {free ? 'YES' : 'NO'}, Queue Length: {response.queueLength}</span>
                    </Card.Meta>
                    {Object.keys(tree).map((dir, i) => 
                        <FileList 
                            key={i}
                            directoryName={dir} 
                            files={tree[dir]}
                            disabled={downloadRequest === 'inProgress'}
                            onSelectionChange={this.onFileSelectionChange}
                        />
                    )}
                </Card.Content>
                {selectedFiles.length > 0 && <Card.Content extra>
                        <span>
                            <Button 
                                color='green' 
                                content='Download'
                                icon='download' 
                                label={{ 
                                    as: 'a', 
                                    basic: false, 
                                    content: `${selectedFiles.length} file${selectedFiles.length === 1 ? '' : 's'}, ${selectedSize}`
                                }}
                                labelPosition='right'
                                onClick={() => this.download(response.username, selectedFiles)}
                                disabled={downloadRequest === 'inProgress'}
                            />
                            {downloadRequest === 'inProgress' && <Icon loading name='circle notch' size='large'/>}
                            {downloadRequest === 'complete' && <Icon name='checkmark' color='green' size='large'/>}
                            {downloadRequest === 'error' && <span>
                                <Icon name='x' color='red' size='large'/>
                                <Label>{downloadError.data + ` (HTTP ${downloadError.status} ${downloadError.statusText})`}</Label>
                            </span>}
                        </span>
                </Card.Content>}
            </Card>
        )
    }
}

export default Response;

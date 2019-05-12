import React, { Component } from 'react';
import { formatBytes, getDirectoryName } from './util';

import FileList from './FileList'

import { 
    Button, 
    Card, 
    Icon
} from 'semantic-ui-react';

class Response extends Component {
    onFileSelectionChange = (file, state) => {
        console.log(file, state);
    }

    render() {
        let response = this.props.response;
        let free = response.freeUploadSlots > 0;

        let tree = response.files.reduce((dict, file) => {
            let dir = getDirectoryName(file.filename);
            dict[dir] = dict[dir] === undefined ? [ file ] : dict[dir].concat(file);
            return dict;
        }, {});

        return (
            <Card className='result-card'>
                <Card.Content>
                    <Card.Header><Icon name='circle' color={free ? 'green' : 'yellow'}/>{response.username}</Card.Header>
                    <Card.Meta className='result-meta'>
                        <span>Upload Speed: {formatBytes(response.uploadSpeed)}/s, Free Upload Slot: {free ? 'YES' : 'NO'}, Queue Length: {response.queueLength}</span>
                    </Card.Meta>
                    {Object.keys(tree).map(dir => <FileList directoryName={dir} files={tree[dir]} onSelectionChange={this.onFileSelectionChange}/>)}
                </Card.Content>
                <Card.Content extra>
                    <Button color='green' content='Download' icon='download' label={{ as: 'a', basic: false, content: '2 Selected' }} labelPosition='left' />
                </Card.Content>
            </Card>
        )
    }
}

export default Response;

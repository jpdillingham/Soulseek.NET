import React, { Component } from 'react';
import { formatBytes } from './util';

import FileList from './FileList'

import { 
    Button, 
    Card, 
    Icon
} from 'semantic-ui-react';

class Response extends Component {
    render() {
        let response = this.props.response;
        let free = response.freeUploadSlots > 0;

        return (
            <Card className='result-card'>
                <Card.Content>
                    <Card.Header><Icon name='circle' color={free ? 'green' : 'yellow'}/>{response.username}</Card.Header>
                    <Card.Meta className='result-meta'>
                        <span>Upload Speed: {formatBytes(response.uploadSpeed)}/s, Free Upload Slot: {free ? 'YES' : 'NO'}, Queue Length: {response.queueLength}</span>
                    </Card.Meta>
                    <FileList files={response.files}/>
                </Card.Content>
                <Card.Content extra>
                    <Button color='green' content='Download' icon='download' label={{ as: 'a', basic: false, content: '2 Selected' }} labelPosition='left' />
                </Card.Content>
            </Card>
        )
    }
}

export default Response;

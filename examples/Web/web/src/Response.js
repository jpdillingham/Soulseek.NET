import React, { Component } from 'react';
import { formatSeconds, formatBytes } from './util';
import { Input, Button, Card, Table, Icon, List } from 'semantic-ui-react';
import FileList from './FileList'

class Response extends Component {
    render() {
        let response = this.props.response;
        let free = response.freeUploadSlots > 0;

        return (
            <Card className='resultCard'>
                <Card.Content>
                    <Card.Header><Icon name='circle' color={free ? 'green' : 'yellow'}/>{response.username}</Card.Header>
                    <Card.Meta className='resultMeta'>
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

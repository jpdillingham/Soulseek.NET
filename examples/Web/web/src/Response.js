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
                    <Card.Meta>
                        <span className='date'>{free ? 'Slot available' : 'Queued' }</span>
                    </Card.Meta>
                    <FileList files={response.files}/>
                </Card.Content>
                <Card.Content extra>
                    <a>
                        <Icon name='user' />
                        Average Bitrate
                    </a>
                </Card.Content>
            </Card>
        )
    }
}

export default Response;

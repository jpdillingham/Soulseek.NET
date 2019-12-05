import React, { Component } from 'react';
import axios from 'axios';

import {
    Card
} from 'semantic-ui-react';

import { BASE_URL } from './constants';
import TransferList from './TransferList';

class Transfers extends Component {
    state = { fetchState: '', downloads: [], interval: undefined }

    componentDidMount = () => {
        this.fetch();
        this.setState({ interval: window.setInterval(this.fetch, 500) });
    }

    componentWillUnmount = () => {
        clearInterval(this.state.interval);
        this.setState({ interval: undefined });
    }

    fetch = () => {
        this.setState({ fetchState: 'pending' }, () => {
            axios.get(BASE_URL + `/transfers/${this.props.direction}`)
            .then(response => this.setState({ 
                fetchState: 'complete', downloads: response.data
            }))
            .catch(err => this.setState({ fetchState: 'failed' }))
        })
    }
    
    render = () => {
        let { downloads } = this.state;

        return (
            downloads.length === 0 ? <span>No downloads.</span> :
            <div className='transfer-segment'>
                {downloads.map((user, index) => 
                    <Card key={index} className='transfer-card' raised>
                        <Card.Content>
                            <Card.Header>{user.username}</Card.Header>
                            {user.directories && user.directories.map((dir, index) => 
                                <TransferList 
                                    key={index} 
                                    username={user.username} 
                                    directoryName={dir.directory}
                                    files={dir.files}
                                    direction={this.props.direction}
                                />
                            )}
                        </Card.Content>
                    </Card>
                )}
                <div>&nbsp;</div>
            </div>
        );
    }
}

export default Transfers;
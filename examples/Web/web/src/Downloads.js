import React, { Component } from 'react';
import axios from 'axios';

import {
    Card
} from 'semantic-ui-react';

import { BASE_URL } from './constants';
import DownloadList from './DownloadList';

class Downloads extends Component {
    state = { fetchState: '', downloads: [] }

    componentDidMount = () => {
        this.fetch();
        window.setInterval(this.fetch, 500);
    }

    fetch = () => {
        this.setState({ ...this.state, fetchState: 'pending' }, () => {
            axios.get(BASE_URL + '/files')
            .then(response => this.setState({ 
                fetchState: 'complete', downloads: response.data
            }))
            .catch(err => this.setState({ ...this.state, fetchState: 'failed' }))
        })
    }
    
    render = () => {
        let { downloads } = this.state;

        return (
            downloads && <div>
                {downloads.map((user, index) => 
                    <Card key={index} className='download-card'>
                        <Card.Content>
                            <Card.Header>{user.username}</Card.Header>
                            {user.directories && user.directories.map((dir, index) => 
                                <DownloadList key={index} directoryName={dir.directory} files={dir.files}/>
                            )}
                        </Card.Content>
                    </Card>
                )}
            </div>
        );
    }
}

export default Downloads;
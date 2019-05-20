import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';
import { formatBytes, getFileName } from './util';

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
            downloads && <ul>
                {downloads.map((user, index) => 
                    <li>
                        {user.username}
                        <ul>
                            {user.directories.map((dir, index) => 
                                <li>
                                    {dir.directory}
                                    <ul>
                                            <table>
                                                <tbody>
                                        {dir.files.map((file, index) => 
                                                    <tr>
                                                        <td>{file.filename}</td>
                                                        <td>{file.state}</td>
                                                        <td>{file.percentComplete}</td>
                                                    </tr>
                                        )}
                                        </tbody>
                                    </table>
                                    </ul>
                                </li>
                            )}
                        </ul>
                    </li>
                )}
            </ul>
        );
    }
}

export default Downloads;
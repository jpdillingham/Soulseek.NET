import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';
import { formatBytes, getDirectoryName, getFileName } from './util';

const buildTree = (files) => {
    return files.reduce((dict, file) => {
        let dir = getDirectoryName(file.filename);
        let selectable = { selected: false, ...file };
        dict[dir] = dict[dir] === undefined ? [ selectable ] : dict[dir].concat(selectable);
        return dict;
    }, {});
}

class Downloads extends Component {
    state = { fetchState: '', downloads: [] }

    componentDidMount = () => {
        this.fetch();
    }

    fetch = () => {
        this.setState({ fetchState: 'pending' }, () => {
            axios.get(BASE_URL + '/files')
            .then(response => this.setState({ 
                fetchState: 'complete', downloads: response.data.map(u => [{ username: u.username, files: buildTree(u.files) }])
            }))
            .catch(err => this.setState({ fetchState: 'failed' }))
        })
    }
    
    render = () => {
        let { fetchState, downloads } = this.state;

        console.log(JSON.stringify(downloads));

        return (
            fetchState === 'complete' && downloads && <div>
                {Object.keys(downloads).map((user, index) => 
                    Object.keys(downloads[user]).map((file, index) => 
                        JSON.stringify(downloads[user][file])
                    )
                )}
            </div>
        );
    }
}

export default Downloads;
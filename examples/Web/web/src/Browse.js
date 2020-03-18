import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';

import { 
    Segment, 
    Input, 
    Loader,
} from 'semantic-ui-react';

const initialState = { 
    username: '', 
    browseState: 'idle', 
    browseStatus: 0, 
    response: [], 
    interval: undefined,
};

class Browse extends Component {
    state = initialState;

    browse = () => {
        let username = this.inputtext.inputRef.current.value;

        this.setState({ username , browseState: 'pending' }, () => {
            axios.get(BASE_URL + `/user/${this.state.username}/browse`)
                .then(response => this.setState({ response: response.data }))
                .then(() => this.setState({ browseState: 'complete' }, () => {
                    this.saveState();
                    this.setUsername();
                }))
        });
    }

    clear = () => {
        this.setState(initialState, () => {
            this.saveState();
            this.setUsername();
        });
    }

    onUsernameChange = (event, data) => {
        this.setState({ username: data.value });
    }

    saveState = () => {
        localStorage.setItem('soulseek-example-browse-state', JSON.stringify(this.state));
    }

    loadState = () => {
        this.setState(JSON.parse(localStorage.getItem('soulseek-example-browse-state')) || initialState);
    }

    componentDidMount = () => {
        this.fetchStatus();
        this.loadState();
        this.setState({ 
            interval: window.setInterval(this.fetchStatus, 500)
        }, () => this.setUsername());
    }

    setUsername = () => {
        this.inputtext.inputRef.current.value = this.state.username;
        this.inputtext.inputRef.current.disabled = this.state.browseState !== 'idle';
    }

    componentWillUnmount = () => {
        clearInterval(this.state.interval);
        this.setState({ interval: undefined });
    }

    fetchStatus = () => {
        if (this.state.browseState === 'pending') {
            axios.get(BASE_URL + `/user/${this.state.username}/browse/status`)
                .then(response => this.setState({
                    browseStatus: response.data
                }));
        }
    }

    getDirectoryTree = (directories) => {
        if (directories.length === 0 || directories[0].directoryName === undefined) {
            return [];
        }

        // determine separator
        const sep = directories[0].directoryName.includes('\\') ? '\\' : '/';
        console.log(`using path separator ${sep}`);

        // find the depth of the topmost result
        const depth = Math.min.apply(null, directories.map(d => d.directoryName.split(sep).length));
        console.log(`minimum root depth ${depth}`);

        // find top level directories
        const topLevelDirs = directories
            .filter(d => d.directoryName.split(sep).length === depth);

        return topLevelDirs.map(d => this.getChildDirectories(directories, d, depth));
    }

    getChildDirectories = (directories, root, depth) => {
        console.log(`fetching children`, directories, root, depth);
        const sep = directories[0].directoryName.includes('\\') ? '\\' : '/';

        const children = directories
            .filter(d => d.directoryName !== root.directoryName)
            .filter(d => d.directoryName.split(sep).length === depth + 1)
            .filter(d => d.directoryName.startsWith(root.directoryName));

        return { ...root, children: children.map(c => this.getChildDirectories(directories, c, depth + 1)) };
    }

    render = () => {
        let { browseState, browseStatus, response } = this.state;
        let pending = browseState === 'pending';

        let tree = this.getDirectoryTree(response);

        return (
            <div>
                <Segment className='search-segment' raised>
                    <Input 
                        size='big'
                        ref={input => this.inputtext = input}
                        loading={pending}
                        disabled={pending}
                        className='search-input'
                        placeholder="Enter username to browse..."
                        action={!pending && (browseState === 'idle' ? { content: 'Browse', onClick: this.browse } : { content: 'Clear Results', color: 'red', onClick: this.clear })} 
                    />
                </Segment>
                {pending ? 
                    <Loader 
                        className='search-loader'
                        active 
                        inline='centered' 
                        size='big'
                    >
                        {JSON.stringify(browseStatus)}
                    </Loader>
                : 
                    <div>
                        <pre>{JSON.stringify(tree, null, 2)}</pre>
                    </div>}
                <div>&nbsp;</div>
            </div>
        )
    }
}

export default Browse;
import React, { Component } from 'react';
import axios from 'axios';

import { BASE_URL } from './constants';

import { 
    Segment, 
    Input, 
    Loader,
    Card,
    Container,
    Grid,
    List
} from 'semantic-ui-react';

const initialState = { 
    username: '', 
    browseState: 'idle', 
    browseStatus: 0, 
    response: [], 
    interval: undefined,
    selected: {},
    tree: []
};

class Browse extends Component {
    state = initialState;

    browse = () => {
        let username = this.inputtext.inputRef.current.value;

        this.setState({ username , browseState: 'pending' }, () => {
            axios.get(BASE_URL + `/user/${this.state.username}/browse`)
                .then(response => this.setState({ response: response.data, tree: this.getDirectoryTree(response.data) }))
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

    selectDirectory = (event, value) => {
        this.setState({ selected: value }, () => this.saveState())
    }

    renderDirectoryTree = (directoryTree) => {
        return (directoryTree || []).map(d => (
            <List className='browse-folderlist-list'>
                <List.Item>
                    <List.Icon name='folder'/>
                    <List.Content>
                        <List.Header onClick={(event) => this.selectDirectory(event, d)}>{d.directoryName.split('\\').pop().split('/').pop()}</List.Header>
                        <List.List>
                            {this.renderDirectoryTree(d.children)}
                        </List.List>
                    </List.Content>
                </List.Item>
            </List>
        ))
    }

    render = () => {
        let { browseState, browseStatus, response, tree } = this.state;
        let pending = browseState === 'pending';

        //let tree = this.getDirectoryTree(response);

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
                    <Grid className='browse-results'>
                        <Grid.Row>
                            <Grid.Column width={6} style={{paddingLeft: 0}}>
                                <Card className='browse-folderlist' raised>
                                    {this.renderDirectoryTree(tree)}
                                </Card>
                            </Grid.Column>
                            <Grid.Column width={10} style={{paddingRight: 0}}>
                                <Card className='browse-filelist' raised>
                                    {(this.state.selected.files|| []).map(f => (<li>{f.filename}</li>))}
                                </Card>
                            </Grid.Column>
                        </Grid.Row>
                    </Grid>}
            </div>
        )
    }
}

export default Browse;
import React, { Component } from 'react';
import axios from 'axios';

import './Browse.css';
import { BASE_URL } from '../constants';

import DirectoryTree from './DirectoryTree';

import { 
  Segment, 
  Input, 
  Loader,
  Card,
  Grid,
} from 'semantic-ui-react';

import Directory from './Directory';

const initialState = { 
  username: '', 
  browseState: 'idle', 
  browseStatus: 0,
  browseError: undefined,
  interval: undefined,
  selectedDirectory: {},
  selectedFiles: [],
  tree: []
};

class Browse extends Component {
  state = initialState;

  browse = () => {
    let username = this.inputtext.inputRef.current.value;

    this.setState({ username , browseState: 'pending', browseError: undefined }, () => {
      axios.get(BASE_URL + `/user/${this.state.username}/browse`)
        .then(response => this.setState({ tree: this.getDirectoryTree(response.data) }))
        .then(() => this.setState({ browseState: 'complete', browseError: undefined }, () => {
          this.saveState();
        }))
        .catch(err => this.setState({ browseState: 'error', browseError: err }))
    });
  }

  clear = () => {
    this.setState(initialState, () => {
      this.saveState();
    });
  }

  onUsernameChange = (event, data) => {
    this.setState({ username: data.value });
  }

  saveState = () => {
    this.inputtext.inputRef.current.value = this.state.username;
    this.inputtext.inputRef.current.disabled = this.state.browseState !== 'idle';
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
    }, () => this.saveState());
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

    const separator = directories[0].directoryName.includes('\\') ? '\\' : '/';
    const depth = Math.min.apply(null, directories.map(d => d.directoryName.split(separator).length));

    const topLevelDirs = directories
      .filter(d => d.directoryName.split(separator).length === depth);

    return topLevelDirs.map(directory => this.getChildDirectories(directories, directory, separator, depth));
  }

  getChildDirectories = (directories, root, separator, depth) => {
    const children = directories
      .filter(d => d.directoryName !== root.directoryName)
      .filter(d => d.directoryName.split(separator).length === depth + 1)
      .filter(d => d.directoryName.startsWith(root.directoryName));

    return { ...root, children: children.map(c => this.getChildDirectories(directories, c, separator, depth + 1)) };
  }

  onDirectorySelectionChange = (event, value) => {
    this.setState({ selectedDirectory: { ...value, children: [] }}, () => this.saveState());
  }

  render = () => {
    const { browseState, browseStatus, browseError, tree, selectedDirectory, username } = this.state;
    const pending = browseState === 'pending';

    const emptyTree = !(tree && tree.length > 0);

    const directoryName = selectedDirectory.directoryName;
    const files = (selectedDirectory.files || []).map(f => ({ ...f, filename: `${directoryName}\\${f.filename}`}));

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
            Downloaded {Math.round(browseStatus.percentComplete || 0)}% of Response
          </Loader>
        : 
          <div>
            {browseError ? 
              <span className='browse-error'>Failed to browse {username}</span> :
              <div>
                {!emptyTree && <Grid className='browse-results'>
                  <Grid.Row className='browse-results-row'>
                    <Card className='browse-folderlist' raised>
                      <DirectoryTree 
                        tree={tree} 
                        selectedDirectoryName={directoryName}
                        onSelect={this.onDirectorySelectionChange}
                      />
                    </Card>
                  </Grid.Row>
                  {directoryName && <Grid.Row className='browse-results-row'>
                    <Directory
                      marginTop={-20}
                      name={directoryName}
                      files={files}
                      username={username}
                    />
                  </Grid.Row>}
                </Grid>}
              </div>
            }
        </div>}
      </div>
    )
  }
}

export default Browse;
import React, { Component } from 'react';

import {
    Card,
    Button
} from 'semantic-ui-react';

import TransferList from './TransferList';

class TransferGroup extends Component {
    state = { selections: new Set() }

    onSelectionChange = (directoryName, file, selected) => {
        const { selections } = this.state;
        const obj = JSON.stringify({ directory: directoryName, filename: file.filename });
        selected ? selections.add(obj) : selections.delete(obj);

        this.setState({ selections });
    }

    isSelected = (directoryName, file) => 
        this.state.selections.has(JSON.stringify({ directory: directoryName, filename: file.filename }));

    getSelectedFiles = () => {
        const { user } = this.props;
        
        return Array.from(this.state.selections)
            .map(s => JSON.parse(s))
            .map(s => user.directories
                .find(d => d.directory === s.directory)
                .files.find(f => f.filename === s.filename))
    }

    isStateRetryable = (state) => state.includes('Completed') && state !== 'Completed, Succeeded';
    isStateCancellable = (state) => ['InProgress', 'Requested', 'Queued', 'Initializing'].find(s => s === state);
    isStateRemovable = (state) => state.includes('Completed');

    anySelectedRetryable = () => this.getSelectedFiles().filter(f => this.isStateRetryable(f.state)).length > 0;
    anySelectedCancellable = () => this.getSelectedFiles().filter(f => this.isStateCancellable(f.state)).length > 0;
    anySelectedRemovable = () => this.getSelectedFiles().filter(f => this.isStateRemovable(f.state)).length > 0;
    
    render = () => {
        const { user } = this.props;

        const selected = this.getSelectedFiles();

        return (
            <Card key={user.username} className='transfer-card' raised>
                <Card.Content>
                    <Card.Header>{user.username}</Card.Header>
                    {user.directories && user.directories
                        .map((dir, index) => 
                        <TransferList 
                            key={index} 
                            username={user.username} 
                            directoryName={dir.directory}
                            files={(dir.files || []).map(f => ({ ...f, selected: this.isSelected(dir.directory, f) }))}
                            onSelectionChange={this.onSelectionChange}
                            direction={this.props.direction}
                        />
                    )}
                </Card.Content>
                <Card.Content extra>
                    {<div>
                        {this.anySelectedRetryable() && <Button onClick={() => console.log(this.getSelectedFiles())}>Retry
                        </Button>}
                        {this.anySelectedCancellable() && <Button onClick={() => console.log(this.getSelectedFiles())}>Cancel
                        </Button>}
                        {this.anySelectedRemovable() && <Button onClick={() => console.log(this.getSelectedFiles())}>Remove
                        </Button>}
                    </div>}
                </Card.Content>
            </Card>
        );
    }
}

export default TransferGroup;
import React, { Component } from 'react';

import {
    Card
} from 'semantic-ui-react';

import TransferList from './TransferList';

class TransferGroup extends Component {
    state = { selections: {} }

    onSelectionChange = (directoryName, file, selected) => {
        const { selections } = this.state;

        selections[directoryName] = selections[directoryName] || {};
        selections[directoryName][file.filename] = selected;

        this.setState({ selections });
    }

    isSelected = (directoryName, file) => {
        const { selections } = this.state;
        return selections && selections[directoryName] && selections[directoryName][file.filename];
    }
    
    render = () => {
        const { user } = this.props

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
                    foo
                </Card.Content>
            </Card>
        );
    }
}

export default TransferGroup;
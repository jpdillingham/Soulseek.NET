import React, { Component } from 'react';
import './App.css';
import FileList from './FileList';
import axios from 'axios';
import { Input, Button, Card, Table, Icon, List } from 'semantic-ui-react';
import data from './data'

const BASE_URL = "http://localhost:5000/api/v1";

class App extends Component {
    state = { searchPhrase: '', results: data }

    search = () => {
        //axios.get(BASE_URL + '/search/' + this.state.searchPhrase)
        //.then(response => this.setState({ results: response.data }))
        this.setState({ results: data })
    }

    render() {
        return (
            <div className="app">
                <Input 
                    placeholder="Enter search phrase..."
                    onChange={(event, data) => this.setState({ searchPhrase: data.value })}
                />
                <Button
                    onClick={this.search}
                >
                    Search
                </Button>
                <div className="results">
                    {this.state.results.sort((a, b) => b.freeUploadSlots - a.freeUploadSlots).map(r =>
                        <FileList response={r}/>
                    )}
                </div>
            </div>
        )
    }
}

export default App;
